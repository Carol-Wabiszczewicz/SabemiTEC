export type StatusProcessamento = "Pendente" | "Processado" | "Erro" | "Duplicado";

export interface EventoPagamento {
  id: string;
  idTransacao: string;
  idContrato: string | null;
  valor: number | null;
  dataPagamento: string | null;
  statusRecebido: string | null;
  statusProcessamento: StatusProcessamento;
  mensagemErro: string | null;
  recebidoEm: string;
  processadoEm: string | null;
}

export interface StatusContratoResumo {
  id: string;
  idContrato: string;
  ultimoIdTransacao: string;
  ultimoValor: number;
  ultimaDataPagamento: string;
  statusAtual: string;
  atualizadoEm: string;
}
