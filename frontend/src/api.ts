import type { EventoPagamento, StatusContratoResumo } from "./types";

const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5080";

export async function buscarPagamentos(filtros: {
  status?: string;
  idContrato?: string;
}): Promise<EventoPagamento[]> {
  const params = new URLSearchParams();
  if (filtros.status) params.set("status", filtros.status);
  if (filtros.idContrato) params.set("idContrato", filtros.idContrato);

  const resposta = await fetch(`${API_URL}/pagamentos?${params.toString()}`);
  if (!resposta.ok) {
    throw new Error(`Falha ao buscar pagamentos: ${resposta.status}`);
  }
  return resposta.json();
}

export async function buscarContratos(): Promise<StatusContratoResumo[]> {
  const resposta = await fetch(`${API_URL}/pagamentos/contratos`);
  if (!resposta.ok) {
    throw new Error(`Falha ao buscar contratos: ${resposta.status}`);
  }
  return resposta.json();
}
