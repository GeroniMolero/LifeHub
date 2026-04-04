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
  @Input({ required: true }) activeTab: 'code' | 'preview' = 'code';
  @Input({ required: true }) renderedPreview = '';
  @Input({ required: true }) loadingDocuments = false;
  @Input({ required: true }) activeVisualMediaReferences: SpaceMediaReference[] = [];
  @Input() localFileBlobUrls: Map<string, string> = new Map();
  @Input() visualLayouts: Map<string, VisualMediaLayout> = new Map();

  @Output() setActiveTab = new EventEmitter<'code' | 'preview'>();
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

  applyBold(editor: HTMLTextAreaElement): void {
    this.wrapSelection(editor, '**', '**', 'texto en negrita');
  }

  applyItalic(editor: HTMLTextAreaElement): void {
    this.wrapSelection(editor, '*', '*', 'texto en cursiva');
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
    const selected = value.slice(start, end) || placeholder;
    const replacement = `${before}${selected}${after}`;
    const nextValue = value.slice(0, start) + replacement + value.slice(end);

    this.setContentValue(nextValue);

    const selectionStart = start + before.length;
    const selectionEnd = selectionStart + selected.length;
    this.restoreSelection(editor, selectionStart, selectionEnd);
  }

  private prefixSelectedLines(editor: HTMLTextAreaElement, getPrefix: (lineIndex: number) => string): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;

    const blockStart = value.lastIndexOf('\n', Math.max(0, start - 1)) + 1;
    const nextBreak = value.indexOf('\n', end);
    const blockEnd = nextBreak === -1 ? value.length : nextBreak;

    const block = value.slice(blockStart, blockEnd);
    const prefixedBlock = block
      .split('\n')
      .map((line, index) => line.trim() ? `${getPrefix(index)}${line}` : line)
      .join('\n');

    const nextValue = value.slice(0, blockStart) + prefixedBlock + value.slice(blockEnd);

    this.setContentValue(nextValue);
    this.restoreSelection(editor, blockStart, blockStart + prefixedBlock.length);
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
