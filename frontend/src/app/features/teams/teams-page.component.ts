import { Component, inject, signal, computed, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, TeamDto } from '../../core/api/api.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-teams-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './teams-page.component.html',
  styleUrl: './teams-page.component.scss',
})
export class TeamsPageComponent {
  private readonly api = inject(ApiService);

  // data
  teams = signal<TeamDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // form
  newName = signal('');

  teamsCount = computed(() => this.teams().length);
  canGenerate = computed(() => this.teamsCount() >= 10 && this.teamsCount() % 2 === 0);

  // confirmation dialog
  @ViewChild('confirmDialog')
  confirmDialog!: ElementRef<HTMLDialogElement>;

  confirmTitle = signal('Confirmation');
  confirmMessage = signal('');
  confirmDanger = signal(false);

  private confirmAction: (() => void) | null = null;

  // lifecycle
  ngOnInit() {
    this.refresh();
  }

  // API calls
  refresh() {
    this.loading.set(true);
    this.error.set(null);

    this.api.getTeams().subscribe({
      next: (data) => {
        this.teams.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Impossible de charger les équipes.');
        this.loading.set(false);
      },
    });
  }

  addTeam() {
    const name = this.newName().trim();
    if (!name) return;

    this.loading.set(true);
    this.error.set(null);

    this.api.createTeam({ name }).subscribe({
      next: () => {
        this.newName.set('');
        this.refresh();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(
          err?.status === 409 ? "Nom d'équipe déjà utilisé." : "Erreur lors de l'ajout."
        );
      },
    });
  }

  // ---- DELETE ONE ----
  askDeleteOne(team: TeamDto) {
    this.openConfirm("Supprimer l'équipe", `Supprimer définitivement “${team.name}” ?`, true, () =>
      this.deleteTeam(team.id)
    );
  }

  private deleteTeam(id: string) {
    this.loading.set(true);
    this.error.set(null);

    this.api.deleteTeam(id).subscribe({
      next: () => this.refresh(),
      error: () => {
        this.loading.set(false);
        this.error.set('Erreur lors de la suppression.');
      },
    });
  }

  // ---- DELETE ALL ----
  askDeleteAll() {
    const count = this.teamsCount();
    if (count === 0) return;

    this.openConfirm(
      'Supprimer toutes les équipes',
      `Tu es sur le point de supprimer ${count} équipe(s). Cette action est irréversible.`,
      true,
      () => this.deleteAllTeams()
    );
  }

  private deleteAllTeams() {
    this.loading.set(true);
    this.error.set(null);

    this.api.deleteAllTeams().subscribe({
      next: () => this.refresh(),
      error: () => {
        this.loading.set(false);
        this.error.set('Erreur lors de la suppression globale.');
      },
    });
  }

  // ---- MODAL ----
  openConfirm(title: string, message: string, danger: boolean, action: () => void) {
    this.confirmTitle.set(title);
    this.confirmMessage.set(message);
    this.confirmDanger.set(danger);
    this.confirmAction = action;
    this.confirmDialog.nativeElement.showModal();
  }

  confirm() {
    this.confirmDialog.nativeElement.close();
    this.confirmAction?.();
    this.confirmAction = null;
  }

  cancel() {
    this.confirmDialog.nativeElement.close();
    this.confirmAction = null;
  }
}
