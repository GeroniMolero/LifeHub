import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

import { Document, DocumentType } from '../../../../models/document.model';
import { SpaceMediaReference } from '../../../../models/space-media-reference.model';

interface VisualMediaLayout {
  x: number;
  y: number;
  width: number;
  zIndex: number;
}

@Component({
  selector: 'app-space-editor-main',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './space-editor-main.component.html',
  styleUrls: ['./space-editor-main.component.scss']
})
export class SpaceEditorMainComponent {
  @Input({ required: true }) selectedDocument: Document | null = null;
  @Input({ required: true }) editDocumentForm!: FormGroup;
  @Input({ required: true }) activeTab: 'code' | 'preview' | 'split' = 'code';
  @Input({ required: true }) renderedPreview = '';
  @Input({ required: true }) loadingDocuments = false;
  @Input({ required: true }) activeVisualMediaReferences: SpaceMediaReference[] = [];
  @Input() localFileBlobUrls: Map<string, string> = new Map();
  @Input() visualLayouts: Map<string, VisualMediaLayout> = new Map();

  @Output() setActiveTab = new EventEmitter<'code' | 'preview' | 'split'>();
  @Output() saveDocument = new EventEmitter<void>();
  @Output() deleteDocument = new EventEmitter<number>();
  @Output() startDraggingMedia = new EventEmitter<{ event: PointerEvent; id: string }>();

  private trustedEmbedCache = new Map<string, SafeResourceUrl>();

  constructor(private sanitizer: DomSanitizer) {}

  isEmbedReference(item: SpaceMediaReference): boolean {
    return item.type === 'external-embed' && !!item.embedUrl;
  }

  isVideoReference(item: SpaceMediaReference): boolean {
    return item.type === 'local-session-file' && !!item.mimeType?.startsWith('video/');
  }

  isImageReference(item: SpaceMediaReference): boolean {
    return item.type === 'local-session-file' && !!item.mimeType?.startsWith('image/');
  }

  getLocalMediaUrl(id: string): string | null {
    return this.localFileBlobUrls.get(id) ?? null;
  }

  toTrustedResource(url: string): SafeResourceUrl {
    if (this.trustedEmbedCache.has(url)) {
      return this.trustedEmbedCache.get(url)!;
    }
    const value = this.sanitizer.bypassSecurityTrustResourceUrl(url);
    this.trustedEmbedCache.set(url, value);
    return value;
  }

  getVisualStyle(id: string): { [key: string]: string } {
    const layout = this.visualLayouts.get(id);
    if (!layout) {
      return { left: '0px', top: '0px', width: '300px', zIndex: '1' };
    }
    return {
      left: `${layout.x}px`,
      top: `${layout.y}px`,
      width: `${layout.width}px`,
      zIndex: `${layout.zIndex}`
    };
  }

  getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
      [DocumentType.List]: 'Lista'
    };
    return type !== undefined ? (typeMap[Number(type)] || 'Nota') : 'Nota';
  }

  onEditorKeyDown(event: KeyboardEvent, editor: HTMLTextAreaElement): void {
    const mod = event.ctrlKey || event.metaKey;

    if (event.key === 'Tab') {
      event.preventDefault();
      this.insertAtCursor(editor, '  ');
      return;
    }

    if (mod && event.key === 'b') {
      event.preventDefault();
      this.applyBold(editor);
      return;
    }

    if (mod && event.key === 'i') {
      event.preventDefault();
      this.applyItalic(editor);
      return;
    }

    if (mod && event.key === 's') {
      event.preventDefault();
      this.saveDocument.emit();
    }
  }

  private insertAtCursor(editor: HTMLTextAreaElement, text: string): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const nextValue = value.slice(0, start) + text + value.slice(end);
    this.setContentValue(nextValue);
    this.restoreSelection(editor, start + text.length, start + text.length);
  }

  clearFormatting(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;

    // Expand outward to include adjacent inline markers (* ~)
    let outerStart = start;
    while (outerStart > 0 && (value[outerStart - 1] === '*' || value[outerStart - 1] === '~')) outerStart--;

    let outerEnd = end;
    while (outerEnd < value.length && (value[outerEnd] === '*' || value[outerEnd] === '~')) outerEnd++;

    const region = value.slice(outerStart, outerEnd);

    // Strip inline markers (* bold/italic/strikethrough, ~ strikethrough)
    // then strip line prefixes (headings, blockquote, lists)
    const inner = region
      .replace(/^[*~]+|[*~]+$/g, '')
      .split('\n')
      .map(line => line
        .replace(/^#{1,6} /, '')
        .replace(/^> /, '')
        .replace(/^- /, '')
        .replace(/^\d+\. /, '')
      )
      .join('\n');

    if (inner === region) return;

    const nextValue = value.slice(0, outerStart) + inner + value.slice(outerEnd);
    this.setContentValue(nextValue);
    this.restoreSelection(editor, outerStart, outerStart + inner.length);
  }

  applyHeading(editor: HTMLTextAreaElement, level: 1 | 2 | 3): void {
    this.prefixSelectedLines(editor, () => '#'.repeat(level) + ' ');
  }

  applyBlockquote(editor: HTMLTextAreaElement): void {
    this.prefixSelectedLines(editor, () => '> ');
  }

  applyBold(editor: HTMLTextAreaElement): void {
    this.wrapSelection(editor, '**', '**', 'texto en negrita');
  }

  applyItalic(editor: HTMLTextAreaElement): void {
    this.wrapSelection(editor, '*', '*', 'texto en cursiva');
  }

  applyStrikethrough(editor: HTMLTextAreaElement): void {
    this.wrapSelection(editor, '~~', '~~', 'texto tachado');
  }

  applyBulletList(editor: HTMLTextAreaElement): void {
    this.prefixSelectedLines(editor, () => '- ');
  }

  applyOrderedList(editor: HTMLTextAreaElement): void {
    this.prefixSelectedLines(editor, (index) => `${index + 1}. `);
  }

  insertLink(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || 'texto del enlace';
    const urlPlaceholder = 'https://ejemplo.com';
    const replacement = `[${selected}](${urlPlaceholder})`;
    const nextValue = value.slice(0, start) + replacement + value.slice(end);

    this.setContentValue(nextValue);

    const urlStart = start + selected.length + 3;
    const urlEnd = urlStart + urlPlaceholder.length;
    this.restoreSelection(editor, urlStart, urlEnd);
  }

  insertImage(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || 'descripción';
    const urlPlaceholder = 'https://url-de-imagen.com';
    const replacement = `![${selected}](${urlPlaceholder})`;
    const nextValue = value.slice(0, start) + replacement + value.slice(end);

    this.setContentValue(nextValue);

    const urlStart = start + selected.length + 4;
    const urlEnd = urlStart + urlPlaceholder.length;
    this.restoreSelection(editor, urlStart, urlEnd);
  }

  insertCodeBlock(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || 'codigo';
    const replacement = `\n\`\`\`\n${selected}\n\`\`\`\n`;
    const nextValue = value.slice(0, start) + replacement + value.slice(end);

    this.setContentValue(nextValue);

    const selectionStart = start + 5;
    const selectionEnd = selectionStart + selected.length;
    this.restoreSelection(editor, selectionStart, selectionEnd);
  }

  private wrapSelection(
    editor: HTMLTextAreaElement,
    before: string,
    after: string,
    placeholder: string
  ): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end);

    // Caso 1: el usuario seleccionó incluyendo los marcadores, ej. "**texto**"
    const innerWrapped =
      selected.startsWith(before) &&
      selected.endsWith(after) &&
      selected.length > before.length + after.length &&
      !selected.startsWith(before + before[0]);

    // Caso 2: los marcadores rodean la selección por fuera, ej. selección "texto" dentro de "**texto**"
    const outerStart = start - before.length;
    const outerEnd = end + after.length;
    const surroundedBy =
      outerStart >= 0 &&
      value.slice(outerStart, start) === before &&
      value.slice(end, outerEnd) === after &&
      (before.length === 1 ? value[outerStart - 1] !== before[0] : true) &&
      (after.length === 1 ? value[outerEnd] !== after[0] : true);

    if (innerWrapped) {
      const unwrapped = selected.slice(before.length, selected.length - after.length);
      const nextValue = value.slice(0, start) + unwrapped + value.slice(end);
      this.setContentValue(nextValue);
      this.restoreSelection(editor, start, start + unwrapped.length);
      return;
    }

    if (surroundedBy) {
      const nextValue = value.slice(0, outerStart) + selected + value.slice(outerEnd);
      this.setContentValue(nextValue);
      this.restoreSelection(editor, outerStart, outerStart + selected.length);
      return;
    }

    const text = selected || placeholder;
    const replacement = `${before}${text}${after}`;
    const nextValue = value.slice(0, start) + replacement + value.slice(end);
    this.setContentValue(nextValue);
    const selectionStart = start + before.length;
    this.restoreSelection(editor, selectionStart, selectionStart + text.length);
  }

  private prefixSelectedLines(editor: HTMLTextAreaElement, getPrefix: (lineIndex: number) => string): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;

    const blockStart = value.lastIndexOf('\n', Math.max(0, start - 1)) + 1;
    const nextBreak = value.indexOf('\n', end);
    const blockEnd = nextBreak === -1 ? value.length : nextBreak;

    const lines = value.slice(blockStart, blockEnd).split('\n');
    const allPrefixed = lines.every((line, i) => !line.trim() || line.startsWith(getPrefix(i)));

    const processedBlock = lines
      .map((line, i) => {
        if (!line.trim()) return line;
        const prefix = getPrefix(i);
        return allPrefixed ? line.slice(prefix.length) : `${prefix}${line}`;
      })
      .join('\n');

    const nextValue = value.slice(0, blockStart) + processedBlock + value.slice(blockEnd);
    this.setContentValue(nextValue);
    this.restoreSelection(editor, blockStart, blockStart + processedBlock.length);
  }

  private getContentValue(): string {
    return String(this.editDocumentForm.get('content')?.value || '');
  }

  private setContentValue(value: string): void {
    const control = this.editDocumentForm.get('content');
    if (!control) return;
    control.setValue(value);
    control.markAsDirty();
    control.markAsTouched();
  }

  private restoreSelection(editor: HTMLTextAreaElement, start: number, end: number): void {
    requestAnimationFrame(() => {
      editor.focus();
      editor.setSelectionRange(start, end);
    });
  }
}
