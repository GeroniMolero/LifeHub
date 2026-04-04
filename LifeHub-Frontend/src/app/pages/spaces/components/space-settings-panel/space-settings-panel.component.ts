import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

import { SpacePrivacy } from '../../../../models/creative-space.model';

@Component({
  selector: 'app-space-settings-panel',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './space-settings-panel.component.html',
  styleUrls: ['./space-settings-panel.component.scss']
})
export class SpaceSettingsPanelComponent {
  @Input({ required: true }) showEditSpace = false;
  @Input({ required: true }) editSpaceForm!: FormGroup;
  @Input({ required: true }) loading = false;

  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  readonly SpacePrivacy = SpacePrivacy;
}
