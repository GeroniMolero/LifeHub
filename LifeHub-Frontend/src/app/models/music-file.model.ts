export interface MusicFile {
  id: number;
  userId: string;
  fileName: string;
  title: string;
  artist?: string;
  album?: string;
  durationSeconds?: number;
  genre?: string;
  localPath?: string;
  createdAt: Date;
}

export interface CreateMusicFileRequest {
  fileName: string;
  title: string;
  artist?: string;
  album?: string;
  durationSeconds?: number;
  genre?: string;
  localPath?: string;
}

export interface UpdateMusicFileRequest {
  title: string;
  artist?: string;
  album?: string;
  durationSeconds?: number;
  genre?: string;
}
