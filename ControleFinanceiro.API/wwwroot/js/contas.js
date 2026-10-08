// Cadastro de contas e associação do histórico, sem confirmar realização financeira.
let contas = [];
let contaEmEdicao = null;
const tiposConta = { ContaCorrente: 'Conta corrente', Poupanca: 'Poupança', Carteira: 'Carteira' };

async function respostaConta(response) {
    const data = await response.json();
    if (!response.ok) throw new Error(data.mensagem || Object.values(data.errors || {}).flat().join(' ') || 'Não foi possível salvar.');
    return data;
}

async function carregarContas() {
    try {
        contas = await respostaConta(await fetch('/api/contas'));
        renderContas();
        if (state.dashboard) renderTransactions(state.dashboard.transacoesDoMes);
    } catch (error) {
        document.getElementById('contas-feedback').textContent = 'Não foi possível carregar as contas. Reabra a página para tentar novamente.';
    }
}

function renderContas() {
    const lista = document.getElementById('contas-lista');
    lista.replaceChildren();
    if (!contas.length) lista.textContent = 'Nenhuma conta cadastrada. Cadastre uma para registrar lançamentos comuns.';
    contas.forEach(conta => {
        const item = document.createElement('li');
        const texto = document.createElement('span');
        texto.textContent = `${conta.nome} — ${tiposConta[conta.tipo]} (${conta.ativa ? 'Ativa' : 'Inativa'})`;
        const editar = document.createElement('button');
        editar.type = 'button'; editar.className = 'btn'; editar.textContent = 'Editar';
        editar.addEventListener('click', () => {
            contaEmEdicao = conta.id;
            document.getElementById('conta-nome').value = conta.nome;
            document.getElementById('conta-tipo').value = conta.tipo;
            document.getElementById('conta-salvar').textContent = 'Salvar alterações';
            document.getElementById('conta-cancelar').hidden = false;
            document.getElementById('conta-nome').focus();
        });
        const status = document.createElement('button');
        status.type = 'button'; status.className = 'btn'; status.textContent = conta.ativa ? 'Inativar' : 'Reativar';
        status.addEventListener('click', async () => {
            status.disabled = true;
            try {
                await respostaConta(await fetch(`/api/contas/${conta.id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nome: conta.nome, tipo: conta.tipo, ativa: !conta.ativa }) }));
                await atualizarFinanceiro();
                showToast('Situação da conta atualizada. O histórico foi preservado.');
            } catch (error) { showToast(error.message, 'error'); status.disabled = false; }
        });
        item.append(texto, editar, status,
            botao(conta.dataAbertura ? 'Corrigir saldo inicial' : 'Informar saldo inicial', () => abrirAbertura(conta.id)),
            botao('Ver extrato', () => abrirExtrato(conta.id)),
            botao('Revisar lançamentos', () => abrirRevisao(conta.id))); lista.append(item);
    });
    const select = document.getElementById('tx-conta');
    const anterior = select.value;
    select.replaceChildren(new Option('Selecione uma conta ativa', ''));
    contas.filter(c => c.ativa).forEach(c => select.add(new Option(c.nome, c.id)));
    if (contas.some(c => c.ativa && String(c.id) === anterior)) select.value = anterior;
    updateContaVisibility();
    preencherContasFinanceiras();
    preencherContasPrevisoes();
}

function updateContaVisibility() {
    const credito = document.getElementById('tx-metodo').value === 'CartaoCredito';
    const select = document.getElementById('tx-conta');
    select.disabled = credito;
    select.required = !credito;
    document.getElementById('tx-conta-ajuda').textContent = credito
        ? 'Compra no crédito: a conta bancária não é associada nesta etapa.'
        : contas.some(c => c.ativa) ? 'Selecione a conta. O lançamento continuará aguardando revisão e não entrará no saldo.' : 'Cadastre ou reative uma conta para continuar.';
}

function contaHistoricoHtml(t) {
    if (t.estado !== 'NaoReconciliada') return `<span>${escapeHtml(t.contaNome || 'Sem conta')}</span><small class="history-note">${escapeHtml(situacao(t))}</small>`;
    if (t.creditoLegado || t.metodoPagamento === 'CartaoCredito') return '<span>Compra no cartão — fora do saldo</span><small class="history-note">Aguardando revisão</small>';
    const options = contas.map(c => `<option value="${c.id}" ${c.id === t.contaId ? 'selected' : ''}>${escapeHtml(c.nome)}${c.ativa ? '' : ' (inativa)'}</option>`).join('');
    return `<span>${escapeHtml(t.contaNome || 'Sem conta associada')}</span>
        <small class="history-note">Aguardando revisão</small>
        <select id="associar-${t.id}" aria-label="Conta para ${escapeHtml(t.titulo)}"><option value="">Selecione uma conta</option>${options}</select>
        <button type="button" class="btn" data-associar="${t.id}">Informar conta</button>`;
}

function cancelarEdicaoConta() {
    contaEmEdicao = null;
    document.getElementById('form-conta').reset();
    document.getElementById('conta-salvar').textContent = 'Cadastrar conta';
    document.getElementById('conta-cancelar').hidden = true;
}

document.addEventListener('DOMContentLoaded', () => {
    carregarContas();
    document.getElementById('conta-cancelar').addEventListener('click', cancelarEdicaoConta);
    document.getElementById('form-conta').addEventListener('submit', async event => {
        event.preventDefault();
        const nome = document.getElementById('conta-nome').value.trim();
        if (!nome) { showToast('Informe o nome da conta.', 'error'); return; }
        const tipo = document.getElementById('conta-tipo').value;
        const existente = contas.find(c => c.id === contaEmEdicao);
        const button = document.getElementById('conta-salvar'); button.disabled = true;
        try {
            await respostaConta(await fetch(existente ? `/api/contas/${existente.id}` : '/api/contas', {
                method: existente ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(existente ? { nome, tipo, ativa: existente.ativa } : { nome, tipo })
            }));
            cancelarEdicaoConta();
            await atualizarFinanceiro();
            showToast('Conta salva.');
            document.getElementById('contas-feedback').textContent = 'Conta salva. Use “Informar saldo inicial” ao lado da conta para começar o acompanhamento.';
        } catch (error) { showToast(error.message, 'error'); }
        finally { button.disabled = false; }
    });
    document.getElementById('transactions-list').addEventListener('click', async event => {
        const button = event.target.closest('[data-associar]');
        if (!button) return;
        const id = button.dataset.associar;
        const contaId = Number(document.getElementById(`associar-${id}`).value);
        if (!contaId) { showToast('Selecione uma conta para associar.', 'error'); return; }
        button.disabled = true;
        try {
            await respostaConta(await fetch(`/api/transacoes/${id}/conta`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ contaId }) }));
            await atualizarFinanceiro();
            showToast('Conta associada. Isso não confirma pagamento ou recebimento.');
        } catch (error) { showToast(error.message, 'error'); button.disabled = false; }
    });
});
