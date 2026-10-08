/* ═══════════════════════════════════════════════════════════
   CONTROLE FINANCEIRO — App Logic
   ═══════════════════════════════════════════════════════════ */

const API = '';  // Mesma origem — servido pelo próprio .NET

// ── State ──
const state = {
    mes: new Date().getMonth() + 1,
    ano: new Date().getFullYear(),
    tipo: 'Despesa',
    filtroMetodo: 'all',
    dashboard: null
};

const MESES = [
    '', 'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'
];

// ═══════════════════════════════════════════════════════════
// INIT
// ═══════════════════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', () => {
    initForm();
    initMonthSelector();
    initFilters();
    initSalaryForm();
    setDefaultDate();
    loadDashboard();
});

// ═══════════════════════════════════════════════════════════
// API CALLS
// ═══════════════════════════════════════════════════════════

async function loadDashboard() {
    try {
        const res = await fetch(`${API}/api/dashboard?mes=${state.mes}&ano=${state.ano}`);
        if (!res.ok) throw new Error('Erro ao carregar dashboard');
        state.dashboard = await res.json();
        renderDashboard();
    } catch (err) {
        console.error(err);
        showToast('Erro ao carregar dados do dashboard', 'error');
    }
}

async function createTransaction(data) {
    try {
        const res = await fetch(`${API}/api/transacoes`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.mensagem || 'Erro ao criar transação');
        }
        const result = await res.json();
        const count = Array.isArray(result) ? result.length : 1;
        showToast(`Transação criada com sucesso! ${count > 1 ? `(${count} parcelas)` : ''}`, 'success');
        await loadDashboard();
        await carregarRevisao();
        return true;
    } catch (err) {
        showToast(err.message, 'error');
        return false;
    }
}

async function saveSalary(data) {
    try {
        const res = await fetch(`${API}/api/salario`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!res.ok) throw new Error('Erro ao salvar salário');
        showToast('Salário atualizado com sucesso!', 'success');
        loadDashboard();
        return true;
    } catch (err) {
        showToast(err.message, 'error');
        return false;
    }
}

// ═══════════════════════════════════════════════════════════
// RENDER
// ═══════════════════════════════════════════════════════════

function renderDashboard() {
    const d = state.dashboard;
    if (!d) return;
    document.getElementById('resumo-aviso').textContent = d.aviso;
    renderFinanceiro(d.financeiro);
    renderResumoPrevisoes(d.previsoes);

    // ── Cards ──
    document.getElementById('card-saldo').textContent = formatCurrency(d.saldoConta);
    document.getElementById('card-recebido').textContent = formatCurrency(d.recebidoNoMes);
    document.getElementById('card-gasto').textContent = formatCurrency(d.gastoNoMes);
    document.getElementById('card-previsto').textContent = formatCurrency(d.previstoParaGastar);

    // ── Month Label ──
    document.getElementById('month-label').textContent = `${MESES[state.mes]} ${state.ano}`;

    // ── Salary ──
    renderSalary(d.salarioAtual);

    // ── Categories ──
    renderCategories(d.gastosPorCategoria);

    // ── Forecast ──
    renderForecast(d.previsaoProximosMeses);

    // ── Transactions ──
    renderTransactions(d.transacoesDoMes);
}

function renderSalary(salary) {
    const el = document.getElementById('salary-display');
    if (!salary) {
        el.innerHTML = '<div class="salary-display__empty">Nenhum salário configurado.<br>Defina abaixo 👇</div>';
        return;
    }
    el.innerHTML = `
        <div class="salary-display__value">${formatCurrency(salary.valor)}</div>
        <div class="salary-display__label">por mês • dia ${salary.diaPagamento} de cada mês</div>
    `;
    // Preencher o form com os valores atuais
    document.getElementById('sal-valor').placeholder = salary.valor;
    document.getElementById('sal-dia').placeholder = salary.diaPagamento;
}

function renderCategories(categories) {
    const el = document.getElementById('category-chart');
    if (!categories || categories.length === 0) {
        el.innerHTML = '<div class="empty-state"><div class="empty-state__text">Sem despesas neste mês</div></div>';
        return;
    }

    const maxVal = Math.max(...categories.map(c => c.total));

    el.innerHTML = categories.map((cat, i) => {
        const pct = maxVal > 0 ? (cat.total / maxVal * 100) : 0;
        const colorClass = `cat-color-${i % 7}`;
        return `
            <div class="category-item">
                <span class="category-item__label" title="${escapeHtml(cat.categoria)}">${escapeHtml(cat.categoria)}</span>
                <div class="category-item__bar-bg">
                    <div class="category-item__bar ${colorClass}" style="width: ${pct}%"></div>
                </div>
                <span class="category-item__value">${formatCurrency(cat.total)}</span>
            </div>
        `;
    }).join('');
}

function renderForecast(forecast) {
    const el = document.getElementById('forecast-list');
    if (!forecast || forecast.length === 0 || forecast.every(f => f.totalPrevisto === 0)) {
        el.innerHTML = '<div class="empty-state"><div class="empty-state__text">Sem parcelas futuras programadas 🎉</div></div>';
        return;
    }

    el.innerHTML = forecast.map(f => `
        <div class="forecast-item">
            <div>
                <div class="forecast-item__month">${f.mesNome} ${f.ano}</div>
                <div class="forecast-item__count">${f.quantidadeParcelas} lançamento${f.quantidadeParcelas !== 1 ? 's' : ''}</div>
            </div>
            <div class="forecast-item__value">${formatCurrency(f.totalPrevisto)}</div>
        </div>
    `).join('');
}

function renderTransactions(transactions) {
    const tbody = document.getElementById('transactions-list');
    const emptyEl = document.getElementById('empty-transactions');
    const tableEl = tbody.closest('table');

    // Apply filter
    let filtered = transactions || [];
    if (state.filtroMetodo !== 'all') {
        filtered = filtered.filter(t => t.metodoPagamento === state.filtroMetodo);
    }

    if (filtered.length === 0) {
        tableEl.style.display = 'none';
        emptyEl.style.display = 'block';
        return;
    }

    tableEl.style.display = 'table';
    emptyEl.style.display = 'none';

    tbody.innerHTML = filtered.map(t => {
        const isReceita = t.tipo === 'Receita';
        const valorClass = isReceita ? 'tx-valor--receita' : 'tx-valor--despesa';
        const prefix = isReceita ? '+' : '-';
        const badgeClass = getBadgeClass(t.metodoPagamento);
        const metodoLabel = getMetodoLabel(t.metodoPagamento);
        const parcelaTag = t.parcelaAtual ? `<span class="tx-parcela">${t.parcelaAtual}/${t.numeroParcelas}</span>` : '';

        return `
            <tr>
                <td>${formatDate(t.data)}</td>
                <td><span class="tx-title">${escapeHtml(t.titulo)}</span>${parcelaTag}</td>
                <td>${escapeHtml(t.categoria)}</td>
                <td><span class="tx-badge ${badgeClass}">${metodoLabel}</span></td>
                <td>${contaHistoricoHtml(t)}</td>
                <td style="text-align: right" class="${valorClass}">${prefix} ${formatCurrency(t.valor)}</td>
                <td><button class="btn" onclick="abrirRegistro(${t.id})" title="Conferir este lançamento">Revisar</button></td>
            </tr>
        `;
    }).join('');
}

// ═══════════════════════════════════════════════════════════
// FORM HANDLERS
// ═══════════════════════════════════════════════════════════

function initForm() {
    // Type toggle
    const btnDespesa = document.getElementById('btn-tipo-despesa');
    const btnReceita = document.getElementById('btn-tipo-receita');

    btnDespesa.addEventListener('click', () => {
        state.tipo = 'Despesa';
        btnDespesa.classList.add('toggle-btn--active');
        btnReceita.classList.remove('toggle-btn--active');
        updateParcelasVisibility();
    });

    btnReceita.addEventListener('click', () => {
        state.tipo = 'Receita';
        btnReceita.classList.add('toggle-btn--active');
        btnDespesa.classList.remove('toggle-btn--active');
        updateParcelasVisibility();
    });

    // Payment method change → toggle parcelas
    document.getElementById('tx-metodo').addEventListener('change', updateParcelasVisibility);

    // Form submit
    document.getElementById('form-transacao').addEventListener('submit', async (e) => {
        e.preventDefault();
        const btn = document.getElementById('btn-salvar');
        btn.disabled = true;
        btn.textContent = '⏳ Salvando...';

        const data = {
            titulo: document.getElementById('tx-titulo').value.trim(),
            valor: parseFloat(document.getElementById('tx-valor').value),
            data: new Date(document.getElementById('tx-data').value).toISOString(),
            tipo: state.tipo,
            categoria: document.getElementById('tx-categoria').value,
            metodoPagamento: document.getElementById('tx-metodo').value,
            contaId: document.getElementById('tx-metodo').value === 'CartaoCredito' ? null : Number(document.getElementById('tx-conta').value),
            numeroParcelas: parseInt(document.getElementById('tx-parcelas').value) || 1
        };

        const success = await createTransaction(data);
        btn.disabled = false;
        btn.textContent = '💾 Cadastrar Transação';

        if (success) {
            e.target.reset();
            setDefaultDate();
            state.tipo = 'Despesa';
            document.getElementById('btn-tipo-despesa').classList.add('toggle-btn--active');
            document.getElementById('btn-tipo-receita').classList.remove('toggle-btn--active');
            document.getElementById('tx-parcelas').value = 1;
            updateParcelasVisibility();
        }
    });

    updateParcelasVisibility();
}

function updateParcelasVisibility() {
    updateContaVisibility();
    const metodo = document.getElementById('tx-metodo').value;
    const parcelasGroup = document.getElementById('parcelas-group');
    const parcelasInput = document.getElementById('tx-parcelas');

    if (metodo === 'CartaoCredito' && state.tipo === 'Despesa') {
        parcelasGroup.style.opacity = '1';
        parcelasInput.disabled = false;
    } else {
        parcelasGroup.style.opacity = '0.4';
        parcelasInput.value = 1;
        parcelasInput.disabled = true;
    }
}

function setDefaultDate() {
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('tx-data').value = today;
}

// ── Salary Form ──
function initSalaryForm() {
    document.getElementById('form-salario').addEventListener('submit', async (e) => {
        e.preventDefault();
        const valor = parseFloat(document.getElementById('sal-valor').value);
        const dia = parseInt(document.getElementById('sal-dia').value);

        if (!valor || !dia) return;

        const success = await saveSalary({ valor, diaPagamento: dia });
        if (success) {
            document.getElementById('sal-valor').value = '';
            document.getElementById('sal-dia').value = '';
        }
    });
}

// ═══════════════════════════════════════════════════════════
// MONTH SELECTOR
// ═══════════════════════════════════════════════════════════

function initMonthSelector() {
    document.getElementById('btn-prev-month').addEventListener('click', () => {
        state.mes--;
        if (state.mes < 1) { state.mes = 12; state.ano--; }
        loadDashboard();
    });

    document.getElementById('btn-next-month').addEventListener('click', () => {
        state.mes++;
        if (state.mes > 12) { state.mes = 1; state.ano++; }
        loadDashboard();
    });
}

// ═══════════════════════════════════════════════════════════
// FILTERS
// ═══════════════════════════════════════════════════════════

function initFilters() {
    document.getElementById('tx-filters').addEventListener('click', (e) => {
        const chip = e.target.closest('.filter-chip');
        if (!chip) return;

        document.querySelectorAll('.filter-chip').forEach(c => c.classList.remove('filter-chip--active'));
        chip.classList.add('filter-chip--active');

        state.filtroMetodo = chip.dataset.filter;
        if (state.dashboard) {
            renderTransactions(state.dashboard.transacoesDoMes);
        }
    });
}

// ═══════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════

function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value || 0);
}

function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function formatDate(dateStr) {
    if (!dateStr) return 'Não informada';
    const [ano, mes, dia] = String(dateStr).slice(0, 10).split('-');
    return `${dia}/${mes}/${ano}`;
}

function getBadgeClass(metodo) {
    const map = {
        'Pix': 'tx-badge--pix',
        'CartaoCredito': 'tx-badge--cartao',
        'Debito': 'tx-badge--debito',
        'Dinheiro': 'tx-badge--dinheiro',
        'Transferencia': 'tx-badge--transferencia'
    };
    return map[metodo] || '';
}

function getMetodoLabel(metodo) {
    const map = {
        'Pix': '⚡ Pix',
        'CartaoCredito': '💳 Crédito',
        'Debito': '💳 Débito',
        'Dinheiro': '💵 Dinheiro',
        'Transferencia': '🏦 Transf.'
    };
    return map[metodo] || metodo;
}

function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast toast--${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'toastOut 0.3s ease forwards';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}
