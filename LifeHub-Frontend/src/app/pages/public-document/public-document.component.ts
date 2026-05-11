import { Component, OnInit, SecurityContext } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { marked } from 'marked';

import { DocumentPublicationService } from '../../services/document-publication.service';
import { AllowedWebsiteService } from '../../services/allowed-website.service';
import { PublicDocumentView } from '../../models/document-publication.model';
import { MEDIA_EMBED_ALLOWED_DOMAINS } from '../../config/media-allowlist.config';

@Component({
  selector: 'app-public-document',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './public-document.component.html',
  styleUrls: ['./public-document.component.scss']
})
export class PublicDocumentComponent implements OnInit {
  document: PublicDocumentView | null = null;
  renderedContent = '';
  loading = true;
  error = '';

  private readonly markdownRenderer = new marked.Renderer();
  private readonly allowedDomains: string[] = [...MEDIA_EMBED_ALLOWED_DOMAINS];

  constructor(
    private route: ActivatedRoute,
    private publicationService: DocumentPublicationService,
    private allowedWebsiteService: AllowedWebsiteService,
    private sanitizer: DomSanitizer
  ) {
    this.markdownRenderer.html = ({ text }) => this.escapeHtml(text);

    this.markdownRenderer.link = ({ href, title, text }) => {
      if (!href?.startsWith('https://')) return text;
      const titleAttr = title ? ` title="${title}"` : '';
      return `<a href="${href}"${titleAttr} target="_blank" rel="noopener noreferrer">${text}</a>`;
    };

    this.markdownRenderer.image = ({ href, title, text }) => {
      if (!this.isDomainAllowed(href ?? '')) {
        return `<span class="blocked-image" title="Imagen bloqueada: dominio no permitido">[imagen bloqueada: ${text || href}]</span>`;
      }
      const titleAttr = title ? ` title="${title}"` : '';
      return `<img src="${href}" alt="${text}"${titleAttr}>`;
    };
  }

  ngOnInit(): void {
    const documentId = Number(this.route.snapshot.paramMap.get('documentId'));

    forkJoin({
      doc: this.publicationService.getPublicDocument(documentId),
      domains: this.allowedWebsiteService.getEmbedAllowlist().pipe(catchError(() => of([] as string[])))
    }).subscribe({
      next: ({ doc, domains }) => {
        if (domains.length) this.allowedDomains.splice(0, this.allowedDomains.length, ...domains);
        this.document = doc;
        this.renderedContent = this.renderMarkdown(doc.content ?? '');
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

  private renderMarkdown(markdown: string): string {
    if (!markdown.trim()) return '';

    const rendered = marked.parse(markdown, { async: false, breaks: true, gfm: true, renderer: this.markdownRenderer });
    if (typeof rendered !== 'string') return '';

    const withoutJs = rendered.replace(/(<a\b[^>]*\bhref\s*=\s*["'])\s*javascript:[^"']*(["'][^>]*>)/gi, '$1#$2');

    const withTasks = withoutJs
      .replace(/<li>\s*<input\s[^>]*\bchecked\b[^>]*>\s*/gi, '<li class="task-item"><span class="task-box" aria-label="completada">☑</span> ')
      .replace(/<li>\s*<input\s[^>]*type="checkbox"[^>]*>\s*/gi, '<li class="task-item"><span class="task-box" aria-label="pendiente">☐</span> ');

    return this.sanitizer.sanitize(SecurityContext.HTML, withTasks) || '';
  }

  private isDomainAllowed(url: string): boolean {
    try {
      const { hostname } = new URL(url);
      return this.allowedDomains.some(d => hostname === d || hostname.endsWith('.' + d));
    } catch { return false; }
  }

  private escapeHtml(value: string): string {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }
}
