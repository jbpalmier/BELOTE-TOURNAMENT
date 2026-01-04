import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms'; // 👈 OBLIGATOIRE
import { ApiService, ScheduleResponse, TeamDto } from '../../core/api/api.service';

type TeamMatchView = {
  round: number;
  table: number;
  opponentName: string;
  opponentId: string;
};

@Component({
  selector: 'app-schedule-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './schedule-page.component.html',
  styleUrl: './schedule-page.component.scss',
})
export class SchedulePageComponent {
  private readonly api = inject(ApiService);

  loading = signal(false);
  error = signal<string | null>(null);

  teams = signal<TeamDto[]>([]);
  schedule = signal<ScheduleResponse | null>(null);

  // UI state
  viewMode = signal<'rounds' | 'team'>('rounds');
  selectedTeamId = signal<string | null>(null);

  teamsCount = computed(() => this.teams().length);
  canGenerate = computed(() => this.teamsCount() >= 10 && this.teamsCount() % 2 === 0);

  selectedTeam = computed(() => {
    const id = this.selectedTeamId();
    return this.teams().find((t) => t.id === id) ?? null;
  });

  byTeam = computed(() => {
    const s = this.schedule();
    const team = this.selectedTeam();
    if (!s || !team) return [];

    const res: TeamMatchView[] = [];

    for (const r of s.rounds) {
      for (const m of r.matches) {
        if (m.teamA.id === team.id) {
          res.push({
            round: r.round,
            table: m.table,
            opponentId: m.teamB.id,
            opponentName: m.teamB.name,
          });
        } else if (m.teamB.id === team.id) {
          res.push({
            round: r.round,
            table: m.table,
            opponentId: m.teamA.id,
            opponentName: m.teamA.name,
          });
        }
      }
    }

    return res.sort((a, b) => a.round - b.round);
  });

  async ngOnInit() {
    this.refreshTeams();
  }

  refreshTeams() {
    this.loading.set(true);
    this.error.set(null);

    this.api.getTeams().subscribe({
      next: (data) => {
        this.teams.set(data);
        this.loading.set(false);

        // si l’équipe sélectionnée n’existe plus, on reset
        if (this.selectedTeamId() && !data.some((t) => t.id === this.selectedTeamId())) {
          this.selectedTeamId.set(null);
        }
      },
      error: () => {
        this.error.set('Impossible de charger les équipes.');
        this.loading.set(false);
      },
    });
  }

  generate() {
    this.loading.set(true);
    this.error.set(null);

    this.api.generateSchedule().subscribe({
      next: (data) => {
        this.schedule.set(data);
        this.loading.set(false);

        // Par défaut, on bascule sur vue manches
        this.viewMode.set('rounds');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error ?? 'Erreur lors de la génération du planning.');
      },
    });
  }

  switchView(mode: 'rounds' | 'team') {
    this.viewMode.set(mode);
    if (mode === 'team' && !this.selectedTeamId() && this.teams().length > 0) {
      this.selectedTeamId.set(this.teams()[0].id);
    }
  }

  downloadPdf() {
    const s = this.schedule();
    if (!s) return;

    const payload = {
      tournamentName: 'Tournoi de belote',
      generatedAt: new Date().toISOString(),
      rounds: s.rounds,
    };

    this.loading.set(true);
    this.error.set(null);

    this.api.downloadSchedulePdf(payload).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'planning-belote.pdf';
        a.click();
        window.URL.revokeObjectURL(url);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error ?? "Erreur lors de l'export PDF.");
      },
    });
  }
}
