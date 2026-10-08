let movimentoRevisado = null;
let aberturaVersao = '';
let registroAberto = null;
const el = id => document.getElementById(id);
const situacao = t => ({ NaoReconciliada: 'Aguardando revisão', Confirmada: 'Confirmado', Desconsiderada: 'Desconsiderado' }[t.estado] || t.situacaoFinanceira);
function focar(id) { const target = el(id); target.scrollIntoView({ block: 'center' }); target.focus(); }
function botao(texto, acao) {
    const b = document.createElement('button'); b.type = 'button'; b.className = 'btn'; b.textContent = texto;
    b.addEventListener('click', async () => { b.disabled = true; try { await acao(); } catch (e) { showToast(e.message, 'error'); } finally { b.disabled = false; } }); return b;
}
function abrirAbertura(id = '') {
    el('abertura-painel').open = true; el('ab-conta').value = String(id); carregarAbertura(); focar(id ? 'ab-valor' : 'ab-conta');
}
function abrirExtrato(id = '') { el('ex-conta').value = String(id); focar('ex-conta'); }
function abrirRevisao(id = '', estado = 'NaoReconciliada') {
    el('rev-conta').value = String(id); el('rev-estado').value = estado; carregarRevisao(); focar('revisao-title');
}
function usarMesExtrato() {
    const hoje = dataLocalHoje(), inicio = `${state.ano}-${String(state.mes).padStart(2, '0')}-01`;
    if (inicio > hoje) { el('extrato-resultado').textContent = 'Este mês ainda não começou. Selecione um mês até hoje.'; return; }
    const ultimo = new Date(state.ano, state.mes, 0).getDate();
    el('ex-inicio').value = inicio; el('ex-fim').value = [inicio.slice(0, 8) + ultimo, hoje].sort()[0];
}
function dataLocalHoje() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
function preencherContasFinanceiras() {
    ['ab-conta', 'ex-conta', 'mov-conta', 'rev-conta'].forEach(id => {
        const select = el(id), anterior = select.value;
        select.replaceChildren(new Option(id === 'rev-conta' ? 'Todas as contas, incluindo sem conta' : 'Selecione a conta', ''));
        contas.filter(c => id !== 'mov-conta' || movimentoRevisado || c.ativa).forEach(c =>
            select.add(new Option(c.nome + (c.ativa ? '' : ' (inativa)'), c.id)));
        if ([...select.options].some(o => o.value === anterior)) select.value = anterior;
    });
    carregarAbertura();
    orientarConta();
}
function carregarAbertura() {
    const c = contas.find(c => c.id === Number(el('ab-conta').value));
    aberturaVersao = c?.versao || '';
    el('ab-data').value = c?.dataAbertura || dataLocalHoje();
    el('ab-valor').value = c?.valorAbertura ?? '';
    el('ab-salvar').textContent = c?.dataAbertura ? 'Corrigir saldo inicial' : 'Salvar saldo inicial';
}
function renderFinanceiro(f) {
    if (!f) return;
    const atual = f.atual;
    const total = atual.totalCalculavel == null ? 'Indisponível' : formatCurrency(atual.totalCalculavel);
    const card = (titulo, valor) => `<div class="finance-metric"><span>${escapeHtml(titulo)}</span><strong>${escapeHtml(valor)}</strong></div>`;
    el('finance-resumo').innerHTML = `<div class="finance-metrics">${card('Saldo calculado hoje — ' + formatDate(atual.referencia), total)}${f.posicaoPeriodo ? card('Saldo em ' + formatDate(f.fimPeriodo), f.posicaoPeriodo.totalCalculavel == null ? 'Indisponível' : formatCurrency(f.posicaoPeriodo.totalCalculavel)) + card('Recebimentos no período', formatCurrency(f.entradasConfirmadas)) + card('Pagamentos no período', formatCurrency(f.saidasConfirmadas)) : '<p>O mês selecionado ainda não começou. Não há saldo futuro disponível.</p>'}</div>
        <p>${atual.parcial ? 'Total de hoje parcial: as contas indicadas abaixo sem saldo calculável ficaram fora da soma.' : ''} ${f.posicaoPeriodo?.parcial ? 'A posição do período também é parcial.' : ''}</p>
        <p>${atual.pendentesRevisao} lançamentos aguardam revisão. Eles ainda não entram no saldo.</p>`;
    if (f.posicaoPeriodo?.contasExcluidas?.length) {
        const p = document.createElement('p'); p.textContent = 'Fora do total do período: ' + f.posicaoPeriodo.contasExcluidas.map(c => `${c.nome}: ${c.motivoIndisponibilidade}`).join('; '); el('finance-resumo').append(p);
    }
    const list = el('finance-contas'); list.replaceChildren();
    atual.contas.forEach(c => {
        const li = document.createElement('li');
        const label = document.createElement('span'); label.textContent = `${c.nome}${c.ativa ? '' : ' (inativa)'}: ${c.calculavel ? formatCurrency(c.saldoCalculado) : c.motivoIndisponibilidade}`;
        li.append(label, botao(c.dataAbertura ? 'Corrigir saldo inicial' : 'Informar saldo inicial', () => abrirAbertura(c.contaId)), botao('Ver extrato', () => abrirExtrato(c.contaId)), botao('Revisar lançamentos', () => abrirRevisao(c.contaId)));
        list.append(li);
    });
    if (!atual.contas.length) list.append(botao('Cadastre sua primeira conta', () => focar('conta-nome')));
}
async function atualizarFinanceiro() {
    await carregarContas(); await loadDashboard(); await carregarRevisao();
    await carregarPrevisoes();
    el('extrato-resultado').textContent = 'Dados alterados. Consulte novamente o extrato para atualizar os saldos.';
}
async function enviarFinanceiro(url, method, data) {
    return respostaConta(await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }));
}
function resetMovimento() {
    previsaoRealizando = null;
    movimentoRevisado = null; el('form-realizado').reset(); el('mov-data').value = dataLocalHoje();
    ['mov-titulo','mov-valor','mov-tipo','mov-categoria','mov-metodo'].forEach(id => el(id).disabled = false);
    el('mov-motivo').disabled = true; el('mov-motivo').required = false;
    el('mov-motivo-group').hidden = true; el('mov-cancelar').hidden = true;
    el('mov-feedback').textContent = '';
    atualizarModo();
    preencherContasFinanceiras();
}
function novoMovimento(tipo) { resetMovimento(); el('mov-tipo').value = tipo; atualizarModo(); focar('mov-conta'); }
function atualizarModo() {
    const receita = el('mov-tipo').value === 'Receita', nome = receita ? 'recebimento' : 'pagamento';
    const corrigir = movimentoRevisado?.estado === 'Confirmada';
    el('mov-title').textContent = movimentoRevisado ? (corrigir ? 'Corrigir lançamento confirmado' : 'Confirmar ' + nome) : 'Novo ' + nome;
    el('form-realizado').dataset.modo = movimentoRevisado ? (corrigir ? 'correcao' : 'confirmacao') : (receita ? 'receita' : 'despesa');
    el('mov-data-label').textContent = 'Data do ' + nome;
    el('mov-salvar').textContent = corrigir ? 'Salvar correção' : (movimentoRevisado ? 'Confirmar ' : 'Registrar ') + nome;
    el('mov-ajuda').textContent = movimentoRevisado
        ? `Confira ${movimentoRevisado.titulo}: conta e data. ${corrigir ? 'A correção recalcula os saldos e fica no histórico de alterações.' : 'Confirme somente se o dinheiro realmente foi movimentado.'}`
        : `Registre somente dinheiro que já ${receita ? 'entrou na' : 'saiu da'} conta. Compras no cartão permanecem no cadastro antigo e não são pagamentos bancários.`;
    orientarConta();
}
function orientarConta() {
    const c = contas.find(c => c.id === Number(el('mov-conta').value));
    el('mov-definir-abertura').hidden = !c || !!c.dataAbertura;
    let aviso = '';
    if (c && !c.dataAbertura) aviso = movimentoRevisado ? 'A confirmação será preservada, mas o saldo ficará indisponível até informar o saldo inicial.' : 'Informe o saldo inicial desta conta para continuar. Seus dados digitados serão preservados.';
    if (c?.dataAbertura && el('mov-data').value < c.dataAbertura) aviso = movimentoRevisado ? 'Este lançamento aconteceu antes do início do acompanhamento. Será preservado, mas não entrará no saldo calculado.' : 'A data é anterior ao início do acompanhamento. Confira a data ou utilize a revisão do histórico.';
    if (c && !c.ativa) aviso += ' Conta inativa: permite revisar o histórico, mas não registrar novos pagamentos ou recebimentos.';
    if (!contas.some(c => c.ativa) && !movimentoRevisado) aviso = 'Cadastre ou reative uma conta para registrar pagamentos e recebimentos.';
    el('mov-conta-ajuda').textContent = aviso;
}
async function revisarMovimento(id) {
    previsaoRealizando = null;
    try {
        const t = await respostaConta(await fetch(`/api/transacoes/${id}`));
        if (t.creditoLegado || t.classificacaoPendente || t.estado === 'Desconsiderada') {
            await abrirRegistro(id); return;
        }
        movimentoRevisado = t; preencherContasFinanceiras();
        el('mov-conta').value = t.contaId || ''; el('mov-data').value = t.dataEfetivacao || t.data.slice(0, 10);
        el('mov-titulo').value = t.titulo; el('mov-valor').value = t.valor; el('mov-tipo').value = t.tipo;
        el('mov-categoria').value = t.categoria; el('mov-metodo').value = t.metodoPagamento;
        const corrigir = t.estado === 'Confirmada';
        ['mov-titulo','mov-valor','mov-tipo','mov-categoria','mov-metodo'].forEach(id => el(id).disabled = !corrigir);
        el('mov-motivo').value = ''; el('mov-motivo').disabled = !corrigir; el('mov-motivo').required = corrigir;
        el('mov-motivo-group').hidden = !corrigir;
        el('mov-cancelar').hidden = false;
        el('mov-feedback').textContent = ''; atualizarModo(); focar('mov-conta');
    } catch (error) { showToast(error.message, 'error'); }
}
async function carregarRevisao() {
    try {
        const filtro = new URLSearchParams({ estado: el('rev-estado').value });
        if (el('rev-conta').value) filtro.set('contaId', el('rev-conta').value);
        const items = await respostaConta(await fetch(`/api/transacoes?${filtro}`));
        const list = el('revisao-lista'); list.replaceChildren();
        if (!items.length) list.textContent = 'Nenhum lançamento nesta situação.';
        items.forEach(t => {
            const row = document.createElement('div'); row.className = 'review-item';
            const label = document.createElement('span');
            label.textContent = `${formatDate(t.dataEfetivacao || t.data)} — ${t.titulo} — ${t.tipo === 'Receita' ? 'Recebimento' : 'Pagamento'} ${formatCurrency(t.valor)} — ${t.contaNome || 'Sem conta'} — ${situacao(t)}${t.creditoLegado ? ' (compra no cartão: fora do saldo)' : ''}${t.classificacaoPendente ? ' (transferência entre minhas contas: fora do saldo)' : ''}${t.motivoDesconsideracao ? ' — ' + t.motivoDesconsideracao : ''}`;
            row.append(label);
            const button = (text, action) => row.append(botao(text, action));
            button('Conferir lançamento', () => abrirRegistro(t.id));
            if (t.estado !== 'Desconsiderada') {
                if (!t.creditoLegado && !t.classificacaoPendente) button(t.estado === 'Confirmada' ? 'Corrigir' : 'Confirmar', () => revisarMovimento(t.id));
                if (t.estado === 'NaoReconciliada' && !t.creditoLegado) button(t.classificacaoPendente ? 'Não é transferência entre minhas contas' : 'É transferência entre minhas contas', async () => {
                    await enviarFinanceiro(`/api/transacoes/${t.id}/classificacao`, 'PUT', { transferenciaPropria: !t.classificacaoPendente, versao: t.versao }); await atualizarFinanceiro();
                });
                button('Desconsiderar', async () => { await abrirRegistro(t.id); mostrarDesconsideracao(); });
            }
            list.append(row);
        });
    } catch (error) { el('revisao-lista').textContent = error.message; }
}
async function abrirRegistro(id) {
    try {
        const t = await respostaConta(await fetch(`/api/transacoes/${id}`)); registroAberto = t;
        el('registro-title').textContent = 'Conferir lançamento: ' + t.titulo;
        el('registro-contexto').textContent = `${t.tipo === 'Receita' ? 'Recebimento' : 'Pagamento'} de ${formatCurrency(t.valor)} · Data informada no cadastro: ${formatDate(t.data)}${t.dataEfetivacao ? ' · Data confirmada: ' + formatDate(t.dataEfetivacao) : ''} · ${t.contaNome || 'Sem conta informada'} · ${t.categoria} · ${getMetodoLabel(t.metodoPagamento)} · ${situacao(t)}${t.motivoDesconsideracao ? ' · Motivo: ' + t.motivoDesconsideracao : ''}`;
        el('registro-aviso').textContent = t.creditoLegado ? 'Esta compra no cartão não representa uma saída da conta. Não pode ser confirmada como pagamento bancário aqui.'
            : t.classificacaoPendente ? 'Transferências entre suas contas ainda não são contabilizadas aqui. Este registro fica preservado e fora do saldo.'
            : t.estado === 'NaoReconciliada' ? 'Este lançamento ainda não entra no saldo. Confira os dados e confirme se ele realmente aconteceu. Informar uma conta, por si só, não confirma pagamento ou recebimento.'
            : t.estado === 'Confirmada' ? 'Este lançamento foi confirmado. Seu efeito no saldo depende da data de início do acompanhamento da conta.' : 'Este lançamento foi desconsiderado e não entra no saldo calculado.';
        el('form-desconsiderar').hidden = true; el('form-editar-antigo').hidden = true;
        el('registro-feedback').textContent = ''; el('registro-revisoes').replaceChildren();
        const acoes = el('registro-acoes'); acoes.replaceChildren();
        if (t.estado !== 'Desconsiderada') {
            if (!t.creditoLegado && !t.classificacaoPendente) acoes.append(botao(t.estado === 'Confirmada' ? 'Corrigir lançamento' : (t.tipo === 'Receita' ? 'Confirmar recebimento' : 'Confirmar pagamento'), async () => { el('registro-dialog').close(); await revisarMovimento(t.id); }));
            if (t.estado === 'NaoReconciliada') acoes.append(botao('Editar dados antes de confirmar', () => {
                ['titulo','valor','tipo','categoria'].forEach(k => el('antigo-' + k).value = t[k]);
                el('form-editar-antigo').hidden = false; el('form-desconsiderar').hidden = true; focar('antigo-titulo');
            }));
            acoes.append(botao('Desconsiderar', mostrarDesconsideracao));
        }
        acoes.append(botao('Ver alterações', () => mostrarRevisoes(t.id)));
        if (!el('registro-dialog').open) el('registro-dialog').showModal();
    } catch (e) { showToast(e.message, 'error'); }
}
function mostrarDesconsideracao() {
    el('form-desconsiderar').hidden = false; el('form-editar-antigo').hidden = true;
    el('desconsiderar-motivo').value = ''; focar('desconsiderar-motivo');
}
async function mostrarRevisoes(id) {
    try {
        const revisoes = await respostaConta(await fetch(`/api/transacoes/${id}/revisoes`));
        const list = el('registro-revisoes'); list.replaceChildren();
        if (!revisoes.length) { list.textContent = 'Nenhuma alteração registrada.'; return; }
        const campos = { Titulo: 'Descrição', Valor: 'Valor', Data: 'Data informada no cadastro', DataEfetivacao: 'Data do movimento', ContaId: 'Conta', Tipo: 'Tipo', Categoria: 'Categoria', MetodoPagamento: 'Forma de movimentação', Estado: 'Situação', MotivoDesconsideracao: 'Motivo da desconsideração' };
        const valor = (k, v) => {
            if (v == null) return 'Não informado';
            if (k === 'Valor') return formatCurrency(v);
            if (k === 'Data' || k === 'DataEfetivacao') return formatDate(v);
            if (k === 'ContaId') return contas.find(c => c.id === v)?.nome || `Conta ${v}`;
            if (k === 'Tipo') return ['Recebimento', 'Pagamento'][v] || v;
            if (k === 'Estado') return ['Aguardando revisão', 'Confirmado', 'Desconsiderado'][v] || v;
            if (k === 'MetodoPagamento') return ['Pix', 'Cartão de crédito', 'Débito', 'Dinheiro', 'Transferência'][v] || v;
            return String(v);
        };
        revisoes.forEach(r => {
            const item = document.createElement('article'), p = document.createElement('p');
            p.textContent = `${new Date(r.instante).toLocaleString('pt-BR')} — ${r.motivo}`; item.append(p);
            const antes = JSON.parse(r.antes), depois = JSON.parse(r.depois);
            Object.entries(campos).forEach(([k, label]) => {
                if (antes[k] === depois[k]) return;
                const line = document.createElement('p'); line.textContent = `${label}: ${valor(k, antes[k])} → ${valor(k, depois[k])}`; item.append(line);
            }); list.append(item);
        });
    } catch (e) { el('registro-feedback').textContent = e.message; }
}
document.addEventListener('DOMContentLoaded', () => {
    el('mov-data').value = dataLocalHoje(); el('ex-inicio').value = dataLocalHoje().slice(0, 8) + '01'; el('ex-fim').value = dataLocalHoje();
    el('ab-conta').addEventListener('change', carregarAbertura);
    el('mov-cancelar').addEventListener('click', resetMovimento);
    el('mov-tipo').addEventListener('change', atualizarModo);
    el('mov-conta').addEventListener('change', orientarConta);
    el('mov-data').addEventListener('change', orientarConta);
    el('rev-conta').addEventListener('change', carregarRevisao);
    ['ab-data','mov-data','ex-inicio','ex-fim'].forEach(id => el(id).max = dataLocalHoje());
    el('rev-estado').addEventListener('change', carregarRevisao); carregarRevisao();
    el('form-abertura').addEventListener('submit', async event => {
        event.preventDefault(); const button = event.submitter; button.disabled = true;
        try {
            await enviarFinanceiro(`/api/contas/${el('ab-conta').value}/abertura`, 'PUT', { dataAbertura: el('ab-data').value, valorAbertura: Number(el('ab-valor').value), versao: aberturaVersao });
            await atualizarFinanceiro(); el('ab-feedback').textContent = 'Saldo inicial salvo. Os saldos foram recalculados.';
        } catch (error) { el('ab-feedback').textContent = error.message; } finally { button.disabled = false; }
    });
    el('form-realizado').addEventListener('submit', async event => {
        event.preventDefault(); const button = event.submitter; button.disabled = true;
        const t = movimentoRevisado;
        const p = previsaoRealizando;
        const conta = contas.find(c => c.id === Number(el('mov-conta').value));
        if (!t && conta && !conta.dataAbertura) { orientarConta(); el('mov-definir-abertura').focus(); button.disabled = false; return; }
        const data = { contaId: Number(el('mov-conta').value), dataEfetivacao: el('mov-data').value, titulo: el('mov-titulo').value.trim(), valor: Number(el('mov-valor').value), tipo: el('mov-tipo').value, categoria: el('mov-categoria').value.trim(), metodoPagamento: el('mov-metodo').value, motivo: el('mov-motivo').value, versao: t?.versao };
        try {
            const path = p ? `/api/previsoes/${p.id}/realizacoes` : !t ? '/api/transacoes/realizadas' : `/api/transacoes/${t.id}/${t.estado === 'Confirmada' ? 'correcao' : 'confirmacao'}`;
            if (p) data.versao = p.versao;
            await enviarFinanceiro(path, t?.estado === 'Confirmada' ? 'PUT' : 'POST', data);
            resetMovimento(); await atualizarFinanceiro(); el('mov-feedback').textContent = 'Lançamento salvo. O saldo calculado foi atualizado.';
            if (p) await abrirPrevisao(p.id);
        } catch (error) { el('mov-feedback').textContent = error.message; } finally { button.disabled = false; }
    });
    el('form-extrato').addEventListener('submit', async event => {
        event.preventDefault();
        try {
            const d = await respostaConta(await fetch(`/api/contas/${el('ex-conta').value}/extrato?inicio=${el('ex-inicio').value}&fim=${el('ex-fim').value}`));
            if (d.saldoInicial == null) { el('extrato-resultado').textContent = d.aviso; return; }
            el('extrato-resultado').innerHTML = `<p>${escapeHtml(d.aviso || '')} Saldo inicial: ${formatCurrency(d.saldoInicial)}</p>
                <p>Período: ${formatDate(d.inicio)} a ${formatDate(d.fim)}</p>
                ${d.exibirAbertura ? `<p>Saldo inicial informado em ${formatDate(d.dataAbertura)}: ${formatCurrency(d.valorAbertura)} (não é receita)</p>` : ''}
                ${!d.movimentos.length ? '<p>Nenhum pagamento ou recebimento confirmado neste período.</p>' : ''}
                <div class="table-scroll"><table class="transactions-table"><thead><tr><th>Data</th><th>Descrição</th><th>Entrada</th><th>Saída</th><th>Saldo</th></tr></thead><tbody>${d.movimentos.map(t => `<tr><td>${formatDate(t.dataEfetivacao)}</td><td>${escapeHtml(t.descricao)}</td><td>${formatCurrency(t.entrada)}</td><td>${formatCurrency(t.saida)}</td><td>${formatCurrency(t.saldo)}</td></tr>`).join('')}</tbody></table></div><p>Saldo final: ${formatCurrency(d.saldoFinal)}</p>`;
        } catch (error) { el('extrato-resultado').textContent = error.message; }
    });
    el('form-desconsiderar').addEventListener('submit', async event => {
        event.preventDefault(); const b = event.submitter; b.disabled = true;
        try {
            await enviarFinanceiro(`/api/transacoes/${registroAberto.id}/desconsideracao`, 'POST', { motivo: el('desconsiderar-motivo').value.trim(), versao: registroAberto.versao });
            el('registro-dialog').close(); await atualizarFinanceiro(); showToast('Lançamento desconsiderado. O histórico foi preservado.');
        } catch (e) { el('registro-feedback').textContent = e.message; } finally { b.disabled = false; }
    });
    el('form-editar-antigo').addEventListener('submit', async event => {
        event.preventDefault(); const b = event.submitter; b.disabled = true;
        try {
            const t = registroAberto;
            await enviarFinanceiro(`/api/transacoes/${t.id}`, 'PUT', { titulo: el('antigo-titulo').value.trim(), valor: Number(el('antigo-valor').value), tipo: el('antigo-tipo').value, categoria: el('antigo-categoria').value.trim(), data: t.data, metodoPagamento: t.metodoPagamento, numeroParcelas: t.numeroParcelas });
            await atualizarFinanceiro(); await abrirRegistro(t.id); el('registro-feedback').textContent = 'Dados salvos. O lançamento continua aguardando revisão.';
        } catch (e) { el('registro-feedback').textContent = e.message; } finally { b.disabled = false; }
    });
});
