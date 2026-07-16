import axios from 'axios';

const api = axios.create({ baseURL: '/api' });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export interface LotteryGame {
  id: number;
  name: string;
  gameType: 1 | 2; // 1=Pick3, 2=Pick4
  isActive: boolean;
}

export interface DrawingDetail {
  id: number;
  lotteryGameId: number;
  gameName: string;
  name: string;
  drawingDate: string;
  timeZoneId: string;
  minutesToDraw: number;
}

export interface PickRequest {
  number1: number;
  number2: number;
  number3: number;
  number4: number;
  amount: number;
}

export interface PurchaseRequest {
  drawingDetailId: number;
  dateToPlay: string;
  pickType: 1 | 2; // 1=Straight, 2=Boxed
  picks: PickRequest[];
}

export interface PickEntryDto {
  number1: number;
  number2: number;
  number3: number;
  number4: number;
  pickType: 1 | 2;
  playCount: number;
  amount: number;
  cost: number;
  prize: number;
}

export interface TicketDto {
  id: number;
  drawingDetailId: number;
  drawingName: string;
  dateToPlay: string;
  eventDate: string;
  total: number;
  description: string;
  purchasedAt: string;
  picks: PickEntryDto[];
}

export const lotteryApi = {
  getGames: () => api.get<LotteryGame[]>('/lottery/games').then((r) => r.data),
  getDrawings: (gameId: number) =>
    api.get<DrawingDetail[]>(`/lottery/games/${gameId}/drawings`).then((r) => r.data),
  previewPicks: (request: PurchaseRequest) =>
    api.get<PickEntryDto[]>('/lottery/tickets/preview', { data: request }).then((r) => r.data),
  purchaseTicket: (request: PurchaseRequest) =>
    api.post<TicketDto>('/lottery/tickets', request).then((r) => r.data),
  getMyTickets: (from?: string, to?: string) =>
    api
      .get<TicketDto[]>('/lottery/tickets/my', { params: { from, to } })
      .then((r) => r.data),
  getTicket: (id: number) =>
    api.get<TicketDto>(`/lottery/tickets/${id}`).then((r) => r.data),
};
