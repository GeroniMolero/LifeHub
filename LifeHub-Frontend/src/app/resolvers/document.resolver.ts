import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { Document } from '../models/document.model';
import { DocumentService } from '../services/document.service';

export const documentResolver: ResolveFn<Document> = (route) => {
  const documentService = inject(DocumentService);
  const documentId = Number(route.paramMap.get('id'));

  return documentService.getDocument(documentId);
};