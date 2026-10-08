let previsaoRealizando = null, previsaoAberta = null, previsaoEditando = null, previsaoTipo = 'Despesa', decisaoPrevisao = null;
let candidatosPrevisao = [];
const nomesSituacaoPrevisao = { Aberta: 'Em aberto', ParcialmenteRealizada: 'Parte recebida ou paga', Realizada: 'Valor realizado', SemValor: 'Sem valor a pagar/receber', Encerrada: 'Encerrada', Cancelada: 'Cancelada' };
function preencherContasPrevisoes() {
    ['prev-conta', 'prev-filtro-conta'].forEach(id => {
        const select = el(id), anterior = select.value;
        select.replaceChildren(new Option(id === 'prev-conta' ? 'Sem conta planejada' : 'Todas as contas', ''));
        if (id === 'prev-filtro-conta') select.add(new Option('Sem conta planejada', 'sem'));
        contas.filter(c => id === 'prev-filtro-conta' || c.ativa || c.id === previsaoEditando?.contaId)
            .forEach(c => select.add(new Option(c.nome + (c.ativa ? '' : ' (inativa)'), c.id)));
        if ([...select.options].some(o => o.value === anterior)) select.value = anterior;
    });
}
function renderResumoPrevisoes(resumo) {
    if (!resumo) return;
    const box = el('previsoes-resumo'); box.replaceChildren();
    const aviso = document.createElement('p'); aviso.textContent = resumo.aviso; box.append(aviso);
    const metrics = document.createElement('div'); metrics.className = 'finance-metrics';
    [['Receitas previstas', resumo.receitas, 'receber'], ['Despesas previstas', resumo.despesas, 'pagar']].forEach(([titulo, t, verbo]) => {
        const p = document.createElement('div'); p.className = 'finance-metric';
        p.textContent = titulo + ': original ' + formatCurrency(t.original) + ' · Referência ' + formatCurrency(t.referencia) +
            ' · Já realizado ' + formatCurrency(t.realizado) + ' · Falta ' + verbo + ' ' + formatCurrency(t.restante) + ' · Vencidas neste mês: ' + t.vencidas;
        metrics.append(p);
    }); box.append(metrics);
}
async function carregarPrevisoes() {
    try {
        const filtro = new URLSearchParams();
        const conta = el('prev-filtro-conta').value;
        if (conta) filtro.set(conta === 'sem' ? 'semConta' : 'contaId', conta === 'sem' ? 'true' : conta);
        [['tipo','prev-filtro-tipo'],['situacao','prev-filtro-situacao'],['inicio','prev-filtro-inicio'],['fim','prev-filtro-fim']].forEach(([k,id]) => {
            if (el(id).value) filtro.set(k, el(id).value);
        });
        if (el('prev-filtro-vencida').checked) filtro.set('vencida', 'true');
        const itens = await respostaConta(await fetch('/api/previsoes?' + filtro));
        const lista = el('previsoes-lista'); lista.replaceChildren();
        if (!itens.length) lista.textContent = 'Nenhuma previsão para estes filtros.';
        itens.forEach(p => {
            const row = document.createElement('article'); row.className = 'review-item';
            const text = document.createElement('span');
            text.textContent = formatDate(p.dataPrevista) + ' — ' + p.descricao + ' — ' + (p.contaNome || 'Sem conta planejada') +
                (p.contaAtiva === false ? ' (inativa)' : '') + ' — ' + nomesSituacaoPrevisao[p.situacao] +
                (p.vencida ? ' · Vencida' : '') + ' · Falta ' + (p.tipo === 'Receita' ? 'receber ' : 'pagar ') + formatCurrency(p.valorRestante);
            row.append(text, botao('Abrir previsão', () => abrirPrevisao(p.id))); lista.append(row);
        });
    } catch (e) { el('previsoes-lista').textContent = e.message; }
}
function novaPrevisao(tipo) {
    previsaoEditando = null; previsaoTipo = tipo; el('form-previsao').reset(); preencherContasPrevisoes();
    el('prev-form-title').textContent = tipo === 'Receita' ? 'Espero receber' : 'Espero pagar';
    el('prev-original').disabled = false; el('prev-data').value = dataLocalHoje();
    el('form-previsao').hidden = false; el('prev-feedback').textContent = ''; focar('prev-descricao');
}
function editarPrevisao() {
    const p = previsaoAberta; novaPrevisao(p.tipo); previsaoEditando = p; preencherContasPrevisoes();
    el('prev-form-title').textContent = 'Editar planejamento — valor original preservado';
    el('prev-descricao').value = p.descricao; el('prev-original').value = p.valorPrevistoOriginal; el('prev-original').disabled = true;
    el('prev-data').value = p.dataPrevista; el('prev-conta').value = p.contaId || ''; el('prev-categoria').value = p.categoria;
    el('prev-observacoes').value = p.observacoes || ''; el('prev-mes').value = p.mesCompetencia || ''; el('prev-ano').value = p.anoCompetencia || '';
    el('previsao-dialog').close(); focar('prev-descricao');
}
async function abrirPrevisao(id) {
    const p = await respostaConta(await fetch('/api/previsoes/' + id)); previsaoAberta = p;
    el('prev-dialog-title').textContent = p.descricao;
    el('prev-contexto').textContent = (p.tipo === 'Receita' ? 'Espero receber' : 'Espero pagar') + ' · ' + formatDate(p.dataPrevista) +
        ' · ' + p.categoria + ' · ' + (p.contaNome || 'Sem conta planejada') + (p.contaAtiva === false ? ' (inativa; escolha outra conta para novos movimentos)' : '') +
        (p.mesCompetencia ? ' · Competência ' + String(p.mesCompetencia).padStart(2, '0') + '/' + p.anoCompetencia : '') +
        (p.observacoes ? ' · ' + p.observacoes : '');
    el('prev-valores').textContent = 'Original: ' + formatCurrency(p.valorPrevistoOriginal) + ' · Final: ' + (p.valorFinal == null ? 'Não informado' : formatCurrency(p.valorFinal)) +
        ' · Realizado: ' + formatCurrency(p.valorRealizado) + ' · Restante: ' + formatCurrency(p.valorRestante) +
        ' · Diferença (referência menos realizado): ' + formatCurrency(p.diferenca) + ' · Excedente: ' + formatCurrency(p.excedente) + ' (somente para conferência)';
    el('prev-decisao').textContent = nomesSituacaoPrevisao[p.situacao] + (p.vencida ? ' · Vencida: há valor em aberto após a data prevista.' : '') +
        (p.motivoEncerramento || p.motivoCancelamento ? ' · Motivo: ' + (p.motivoEncerramento || p.motivoCancelamento) : '');
    ['form-prev-final','form-prev-vinculo','form-prev-decisao'].forEach(id => el(id).hidden = true);
    el('prev-dialog-feedback').textContent = ''; el('prev-historico').replaceChildren();
    const lista = el('prev-movimentos'); lista.replaceChildren();
    if (!p.transacoes.length) lista.textContent = 'Nenhum pagamento ou recebimento vinculado.';
    p.transacoes.forEach(t => {
        const row = document.createElement('div'); row.className = 'review-item';
        const label = document.createElement('span'); label.textContent = formatDate(t.dataEfetivacao) + ' — ' + t.titulo + ' — ' + formatCurrency(t.valor) +
            ' — ' + t.contaNome + ' — ' + situacao(t) + (t.estado === 'Desconsiderada' ? ' (fora do realizado)' : '');
        row.append(label, botao('Conferir movimentação', async () => { el('previsao-dialog').close(); await abrirRegistro(t.id); }),
            botao('Remover vínculo', () => prepararDecisaoPrevisao('desvinculacao', t))); lista.append(row);
    });
    const acoes = el('prev-acoes'); acoes.replaceChildren();
    if (p.estado === 'Ativa') {
        acoes.append(botao('Editar planejamento', editarPrevisao),
            botao('Registrar realização parcial', () => realizarPrevisao(false)),
            botao('Registrar realização total', () => realizarPrevisao(true)),
            botao('Informar valor final', () => { el('form-prev-final').hidden = false; el('prev-final').value = p.valorFinal ?? ''; focar('prev-final'); }),
            botao('Vincular movimentação já registrada', prepararVinculoPrevisao),
            botao('Encerrar restante', () => prepararDecisaoPrevisao('encerramento')));
        if (p.valorRealizado === 0) acoes.append(botao('Cancelar previsão', () => prepararDecisaoPrevisao('cancelamento')));
    } else acoes.append(botao('Reabrir previsão', () => prepararDecisaoPrevisao('reabertura')));
    acoes.append(botao('Ver histórico da previsão', historicoPrevisao), botao('Atualizar dados', () => abrirPrevisao(p.id)));
    if (!el('previsao-dialog').open) el('previsao-dialog').showModal();
}
function realizarPrevisao(total) {
    const p = previsaoAberta; novoMovimento(p.tipo); previsaoRealizando = p;
    el('mov-tipo').disabled = true; el('mov-titulo').value = p.descricao; el('mov-categoria').value = p.categoria;
    if (contas.some(c => c.id === p.contaId && c.ativa)) el('mov-conta').value = p.contaId;
    el('mov-valor').value = total && p.valorRestante > 0 ? p.valorRestante : '';
    el('mov-title').textContent = (p.tipo === 'Receita' ? 'Receber: ' : 'Pagar: ') + p.descricao;
    el('mov-ajuda').textContent = 'Vinculado a esta previsão. Já realizado: ' + formatCurrency(p.valorRealizado) +
        '. Confira conta, valor e data. Só confirme se o dinheiro realmente foi movimentado. ' + (total ? 'O restante foi sugerido; confira o valor efetivo.' : '');
    el('mov-cancelar').hidden = false; el('mov-cancelar').textContent = 'Cancelar operação';
    orientarConta(); el('previsao-dialog').close(); focar('mov-conta');
}
async function prepararVinculoPrevisao() {
    const itens = await respostaConta(await fetch('/api/transacoes?estado=Confirmada'));
    candidatosPrevisao = itens.filter(t => !t.previsaoId && t.tipo === previsaoAberta.tipo && !t.creditoLegado && !t.classificacaoPendente);
    const select = el('prev-transacao'); select.replaceChildren(new Option('Selecione uma movimentação', ''));
    candidatosPrevisao.forEach(t => select.add(new Option(formatDate(t.dataEfetivacao) + ' — ' + t.titulo + ' — ' + formatCurrency(t.valor) + ' — ' + t.contaNome, t.id)));
    el('form-prev-vinculo').hidden = false;
    el('prev-dialog-feedback').textContent = candidatosPrevisao.length ? '' : 'Nenhuma movimentação confirmada compatível e sem vínculo. Registre ou revise o movimento primeiro.';
}
function prepararDecisaoPrevisao(acao, t = null) {
    decisaoPrevisao = { acao, t }; el('form-prev-decisao').hidden = false; el('prev-motivo').value = '';
    const textos = {
        encerramento: 'Encerrar o restante mantém os valores e pagamentos anteriores, mas deixa de cobrar o que falta. Não altera o saldo.',
        cancelamento: 'Cancelar preserva o histórico e não movimenta dinheiro. Só é permitido sem realização participante.',
        reabertura: 'Reabrir volta a calcular o valor em aberto com os pagamentos e recebimentos atuais.',
        desvinculacao: 'Remover o vínculo de ' + (t?.titulo || '') + ' preserva a movimentação e seu efeito no saldo. A previsão será recalculada.'
    };
    el('prev-explicacao').textContent = textos[acao];
    el('prev-confirmar-acao').textContent = { encerramento: 'Confirmar encerramento', cancelamento: 'Confirmar cancelamento', reabertura: 'Confirmar reabertura', desvinculacao: 'Confirmar remoção do vínculo' }[acao];
    focar('prev-motivo');
}
async function historicoPrevisao() {
    const historico = await respostaConta(await fetch('/api/previsoes/' + previsaoAberta.id + '/revisoes'));
    const box = el('prev-historico'); box.replaceChildren();
    const nomes = { Criacao: 'Cadastro', Edicao: 'Planejamento atualizado', ValorFinal: 'Valor final', Realizacao: 'Pagamento ou recebimento registrado', Vinculacao: 'Vínculo adicionado', Desvinculacao: 'Vínculo removido', Encerramento: 'Encerramento', Cancelamento: 'Cancelamento', Reabertura: 'Reabertura' };
    const campos = { Descricao: 'Descrição', ValorPrevistoOriginal: 'Original', ValorFinal: 'Final', DataPrevista: 'Data prevista', Categoria: 'Categoria', Observacoes: 'Observações', AnoCompetencia: 'Ano de competência', MesCompetencia: 'Mês de competência', ContaId: 'Conta planejada', Estado: 'Situação', MotivoEncerramento: 'Motivo do encerramento', MotivoCancelamento: 'Motivo do cancelamento', Transacoes: 'Movimentações vinculadas' };
    const valor = (k,v) => v == null ? 'Não informado' : k.startsWith('Valor') ? formatCurrency(v)
        : k === 'DataPrevista' ? formatDate(v) : k === 'ContaId' ? contas.find(c => c.id === v)?.nome || 'Conta preservada'
        : k === 'Estado' ? ['Ativa','Encerrada','Cancelada'][v] : k === 'Transacoes' ? v.length + ' movimentação(ões)' : String(v);
    for (const r of historico) {
        const item = document.createElement('article'), titulo = document.createElement('p');
        titulo.textContent = new Date(r.instante).toLocaleString('pt-BR') + ' — ' + nomes[r.acao] + ' — ' + r.motivo; item.append(titulo);
        const antes = JSON.parse(r.antes), depois = JSON.parse(r.depois);
        Object.entries(campos).forEach(([k,label]) => {
            if (JSON.stringify(antes[k]) === JSON.stringify(depois[k])) return;
            const linha = document.createElement('p'); linha.textContent = label + ': ' + valor(k,antes[k]) + ' → ' + valor(k,depois[k]); item.append(linha);
        }); box.append(item);
    }
    const aviso = document.createElement('p'); aviso.textContent = 'Correções de valores e desconsiderações ficam também em “Conferir movimentação” → “Ver alterações”.'; box.append(aviso);
}
async function salvarAcaoPrevisao(form, acao) {
    const botoes = [...el('previsao-dialog').querySelectorAll('button')];
    botoes.forEach(b => b.disabled = true);
    try { await acao(); await atualizarFinanceiro(); await abrirPrevisao(previsaoAberta.id); }
    catch (e) { el('prev-dialog-feedback').textContent = e.message + ' Se os dados mudaram, use Atualizar dados antes de tentar novamente.'; }
    finally { botoes.forEach(b => b.disabled = false); }
}
document.addEventListener('DOMContentLoaded', () => {
    preencherContasPrevisoes(); carregarPrevisoes();
    el('form-filtro-previsoes').addEventListener('submit', e => { e.preventDefault(); carregarPrevisoes(); });
    el('form-previsao').addEventListener('submit', async e => {
        e.preventDefault(); const b = e.submitter; b.disabled = true;
        const data = { descricao: el('prev-descricao').value.trim(), dataPrevista: el('prev-data').value,
            contaId: el('prev-conta').value ? Number(el('prev-conta').value) : null, categoria: el('prev-categoria').value.trim(),
            observacoes: el('prev-observacoes').value.trim() || null, anoCompetencia: el('prev-ano').value ? Number(el('prev-ano').value) : null,
            mesCompetencia: el('prev-mes').value ? Number(el('prev-mes').value) : null };
        if (previsaoEditando) data.versao = previsaoEditando.versao;
        else { data.tipo = previsaoTipo; data.valorPrevistoOriginal = Number(el('prev-original').value); }
        try {
            const p = await enviarFinanceiro('/api/previsoes' + (previsaoEditando ? '/' + previsaoEditando.id : ''), previsaoEditando ? 'PUT' : 'POST', data);
            el('form-previsao').hidden = true; await atualizarFinanceiro(); await abrirPrevisao(p.id);
        } catch (err) { el('prev-feedback').textContent = err.message; } finally { b.disabled = false; }
    });
    el('form-prev-final').addEventListener('submit', e => { e.preventDefault(); salvarAcaoPrevisao(e.target, () =>
        enviarFinanceiro('/api/previsoes/' + previsaoAberta.id + '/valor-final', 'PUT', { valorFinal: Number(el('prev-final').value), versao: previsaoAberta.versao })); });
    el('form-prev-vinculo').addEventListener('submit', e => { e.preventDefault(); salvarAcaoPrevisao(e.target, () => {
        const t = candidatosPrevisao.find(t => t.id === Number(el('prev-transacao').value));
        return enviarFinanceiro('/api/previsoes/' + previsaoAberta.id + '/transacoes/' + t.id, 'PUT', { versao: previsaoAberta.versao, versaoTransacao: t.versao });
    }); });
    el('form-prev-decisao').addEventListener('submit', e => { e.preventDefault(); salvarAcaoPrevisao(e.target, () => {
        const { acao, t } = decisaoPrevisao;
        return enviarFinanceiro('/api/previsoes/' + previsaoAberta.id + '/' + (t ? 'transacoes/' + t.id + '/' : '') + acao, 'POST',
            { versao: previsaoAberta.versao, versaoTransacao: t?.versao, motivo: el('prev-motivo').value.trim() });
    }); });
});
