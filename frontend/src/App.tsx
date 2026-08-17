import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import { buscarContratos, buscarPagamentos } from "./api";
import type { EventoPagamento, StatusContratoResumo } from "./types";
import "./App.css";

const OPCOES_STATUS = ["Todos", "Pendente", "Processado", "Erro", "Duplicado"];
const INTERVALO_POLLING_MS = 5000;

function formatarData(iso: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleString("pt-BR");
}

function formatarValor(valor: number | null): string {
  if (valor === null) return "-";
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function App() {
  const [eventos, setEventos] = useState<EventoPagamento[]>([]);
  const [contratos, setContratos] = useState<StatusContratoResumo[]>([]);
  const [statusFiltro, setStatusFiltro] = useState("Todos");
  const [idContratoFiltro, setIdContratoFiltro] = useState("");
  const [carregando, setCarregando] = useState(false);
  const [erroConexao, setErroConexao] = useState<string | null>(null);
  const [ultimaAtualizacao, setUltimaAtualizacao] = useState<Date | null>(null);

  const carregarDados = useCallback(async () => {
    setCarregando(true);
    try {
      const [listaEventos, listaContratos] = await Promise.all([
        buscarPagamentos({
          status: statusFiltro === "Todos" ? undefined : statusFiltro,
          idContrato: idContratoFiltro || undefined,
        }),
        buscarContratos(),
      ]);
      setEventos(listaEventos);
      setContratos(listaContratos);
      setErroConexao(null);
      setUltimaAtualizacao(new Date());
    } catch (erro) {
      setErroConexao(
        erro instanceof Error ? erro.message : "Falha desconhecida ao conectar com a API."
      );
    } finally {
      setCarregando(false);
    }
  }, [statusFiltro, idContratoFiltro]);

  useEffect(() => {
    carregarDados();
    const timer = setInterval(carregarDados, INTERVALO_POLLING_MS);
    return () => clearInterval(timer);
  }, [carregarDados]);

  const resumo = useMemo(() => {
    const total = eventos.length;
    const erros = eventos.filter((e) => e.statusProcessamento === "Erro").length;
    const processados = eventos.filter((e) => e.statusProcessamento === "Processado").length;
    const pendentes = eventos.filter((e) => e.statusProcessamento === "Pendente").length;
    return { total, erros, processados, pendentes };
  }, [eventos]);

  return (
    <div className="dashboard">
      <header className="dashboard__header">
        <h1>Sabemi Tec — Painel de Webhooks de Pagamento</h1>
        <p className="dashboard__subtitulo">
          Monitoramento em tempo (quase) real das notificações recebidas do banco parceiro.
        </p>
      </header>

      {erroConexao && (
        <div className="alerta alerta--erro">
          Não foi possível conectar à API ({erroConexao}). Verifique se o backend está rodando.
        </div>
      )}

      <section className="cards">
        <div className="card">
          <span className="card__valor">{resumo.total}</span>
          <span className="card__label">Eventos (filtro atual)</span>
        </div>
        <div className="card card--sucesso">
          <span className="card__valor">{resumo.processados}</span>
          <span className="card__label">Processados</span>
        </div>
        <div className="card card--pendente">
          <span className="card__valor">{resumo.pendentes}</span>
          <span className="card__label">Pendentes (em processamento)</span>
        </div>
        <div className="card card--erro">
          <span className="card__valor">{resumo.erros}</span>
          <span className="card__label">Com erro</span>
        </div>
      </section>

      <section className="filtros">
        <label>
          Status
          <select value={statusFiltro} onChange={(e) => setStatusFiltro(e.target.value)}>
            {OPCOES_STATUS.map((op) => (
              <option key={op} value={op}>
                {op}
              </option>
            ))}
          </select>
        </label>

        <label>
          ID do contrato
          <input
            type="text"
            placeholder="ex: CT-100"
            value={idContratoFiltro}
            onChange={(e) => setIdContratoFiltro(e.target.value)}
          />
        </label>

        <button onClick={carregarDados} disabled={carregando}>
          {carregando ? "Atualizando..." : "Atualizar agora"}
        </button>

        {ultimaAtualizacao && (
          <span className="filtros__ultima-atualizacao">
            Última atualização: {ultimaAtualizacao.toLocaleTimeString("pt-BR")}
          </span>
        )}
      </section>

      <section>
        <h2>Eventos recebidos</h2>
        <div className="tabela-wrapper">
          <table>
            <thead>
              <tr>
                <th>ID Transação</th>
                <th>ID Contrato</th>
                <th>Valor</th>
                <th>Data pagamento</th>
                <th>Status (banco)</th>
                <th>Processamento</th>
                <th>Recebido em</th>
                <th>Processado em</th>
              </tr>
            </thead>
            <tbody>
              {eventos.length === 0 && !carregando && (
                <tr>
                  <td colSpan={8} className="tabela__vazio">
                    Nenhum evento encontrado para os filtros atuais.
                  </td>
                </tr>
              )}
              {eventos.map((evento) => (
                <Fragment key={evento.id}>
                  <tr
                    className={
                      evento.statusProcessamento === "Erro" ? "linha-erro" : undefined
                    }
                  >
                    <td>{evento.idTransacao}</td>
                    <td>{evento.idContrato ?? "-"}</td>
                    <td>{formatarValor(evento.valor)}</td>
                    <td>{formatarData(evento.dataPagamento)}</td>
                    <td>{evento.statusRecebido ?? "-"}</td>
                    <td>
                      <span className={`badge badge--${evento.statusProcessamento.toLowerCase()}`}>
                        {evento.statusProcessamento}
                      </span>
                    </td>
                    <td>{formatarData(evento.recebidoEm)}</td>
                    <td>{formatarData(evento.processadoEm)}</td>
                  </tr>
                  {evento.statusProcessamento === "Erro" && evento.mensagemErro && (
                    <tr className="linha-erro">
                      <td colSpan={8} className="alerta-inline">
                        ⚠ {evento.mensagemErro}
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section>
        <h2>Status atual dos contratos</h2>
        <div className="tabela-wrapper">
          <table>
            <thead>
              <tr>
                <th>ID Contrato</th>
                <th>Última transação</th>
                <th>Último valor</th>
                <th>Última data pagamento</th>
                <th>Status atual</th>
                <th>Atualizado em</th>
              </tr>
            </thead>
            <tbody>
              {contratos.length === 0 && (
                <tr>
                  <td colSpan={6} className="tabela__vazio">
                    Nenhum contrato processado ainda.
                  </td>
                </tr>
              )}
              {contratos.map((c) => (
                <tr key={c.id}>
                  <td>{c.idContrato}</td>
                  <td>{c.ultimoIdTransacao}</td>
                  <td>{formatarValor(c.ultimoValor)}</td>
                  <td>{formatarData(c.ultimaDataPagamento)}</td>
                  <td>
                    <span className={`badge badge--${c.statusAtual.toLowerCase()}`}>
                      {c.statusAtual}
                    </span>
                  </td>
                  <td>{formatarData(c.atualizadoEm)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}

export default App;
