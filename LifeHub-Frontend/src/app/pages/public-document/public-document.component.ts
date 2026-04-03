import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DocumentPublicationService } from '../../services/document-publication.service';
import { PublicDocumentView } from '../../models/document-publication.model';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-public-document',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './public-document.component.html',
  styleUrls: ['./public-document.component.scss']
})
export class PublicDocumentComponent implements OnInit {
  document: PublicDocumentView | null = null;
  loading = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private publicationService: DocumentPublicationService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    const documentId = Number(this.route.snapshot.paramMap.get('documentId'));
    this.publicationService.getPublicDocument(documentId).subscribe({
      next: doc => {
        this.document = doc;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo cargar el documento público.';
        this.loading = false;
      }
    });
  }

  toTrustedResource(url: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }
}
