export interface MessageDto {
  id: number;
  senderId: string;
  receiverId: string;
  content: string;
  sentAt: string;
  isRead: boolean;
}
