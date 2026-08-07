import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-status-chip',
  imports: [MatIconModule],
  templateUrl: './status-chip.html',
  styleUrl: './status-chip.scss',
})
export class StatusChip {
  readonly status = input<string>('Draft');
  readonly cssClass = computed(() => `chip-${this.status().toLowerCase()}`);
}
