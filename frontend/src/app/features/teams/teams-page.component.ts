import { Component, ElementRef, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService, TeamDto } from '../../core/api/api.service';

@Component({
  selector: 'app-teams-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './teams-page.component.html',
  styleUrl: './teams-page.component.scss',
})
export class TeamsPageComponent {
  private readonly api = inject(ApiService);

  // API state
  teams = signal<TeamDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Generator state
  teamCountInput = signal<string>('10'); // texte -> digits only
  generatedNames = signal<string[]>([]);
  bulkError = signal<string | null>(null);

  // Confirm dialog
  @ViewChild('confirmDialog') confirmDialog!: ElementRef<HTMLDialogElement>;
  confirmTitle = signal('Confirmation');
  confirmMessage = signal('');
  confirmDanger = signal(false);
  private confirmAction: (() => void) | null = null;

  // Computeds
  teamsCount = computed(() => this.teams().length);

  // Le tournoi est “prêt” si au moins 10 équipes et nombre pair (côté existant)
  canGenerateSchedule = computed(() => {
    const n = this.teamsCount();
    return n >= 10 && n % 2 === 0;
  });

  // Validation du champ “Nombre d’équipes”
  desiredCount = computed(() => {
    const raw = this.teamCountInput().trim();
    if (!raw) return null;
    const n = Number(raw);
    if (!Number.isInteger(n)) return null;
    return n;
  });

  isDesiredCountValid = computed(() => {
    const n = this.desiredCount();
    return n !== null && n >= 10 && n % 2 === 0;
  });

  ngOnInit() {
    this.refresh();
  }

  // -------------------------
  // Data
  // -------------------------
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

  // -------------------------
  // Generator
  // -------------------------
  onCountInput(ev: Event) {
    // Garder uniquement les chiffres
    const input = ev.target as HTMLInputElement;
    const digitsOnly = (input.value ?? '').replace(/[^\d]/g, '');
    input.value = digitsOnly;
    this.teamCountInput.set(digitsOnly);
  }

  generateList() {
    this.bulkError.set(null);

    const n = this.desiredCount();
    if (n === null || n < 10 || n % 2 !== 0) {
      this.bulkError.set('Le nombre d’équipes doit être un entier pair, minimum 10.');
      return;
    }

    // Par défaut : nom = numéro ("1", "2", ...)
    const names = Array.from({ length: n }, (_, i) => String(i + 1));
    this.generatedNames.set(names);
  }

  updateGeneratedName(index: number, value: string) {
    const list = [...this.generatedNames()];
    list[index] = value;
    this.generatedNames.set(list);
  }

  clearGenerated() {
    this.generatedNames.set([]);
    this.bulkError.set(null);
  }

  private validateGeneratedNames(names: string[]): string | null {
    const trimmed = names.map((n) => (n ?? '').trim()).filter((n) => n.length > 0);

    if (trimmed.length !== names.length) {
      return 'Tous les noms doivent être renseignés (pas de champ vide).';
    }

    // Doublons (case-insensitive)
    const lower = trimmed.map((n) => n.toLowerCase());
    if (new Set(lower).size !== lower.length) {
      return 'Il y a des doublons dans les noms. Corrige-les avant de valider.';
    }

    // Longueur max côté Domain (si tu as mis 40)
    const tooLong = trimmed.find((n) => n.length > 40);
    if (tooLong) return 'Un nom dépasse 40 caractères.';

    return null;
  }

  async createAllGenerated() {
    this.bulkError.set(null);
    this.error.set(null);

    const names = this.generatedNames();
    if (names.length === 0) {
      this.bulkError.set('Génère d’abord une liste d’équipes.');
      return;
    }

    const validationError = this.validateGeneratedNames(names);
    if (validationError) {
      this.bulkError.set(validationError);
      return;
    }

    this.loading.set(true);

    // Création en série (plus stable, évite de spammer l’API)
    for (const name of names) {
      try {
        await firstValueFrom(this.api.createTeam({ name: name.trim() }));
      } catch (e: any) {
        this.loading.set(false);
        this.bulkError.set(
          e?.status === 409
            ? `Conflit : le nom "${name}" existe déjà.`
            : `Erreur lors de la création de "${name}".`
        );
        return;
      }
    }

    this.loading.set(false);
    this.clearGenerated();
    this.refresh();
  }

  // -------------------------
  // Delete (with confirm modal)
  // -------------------------
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

  // -------------------------
  // Modal helpers
  // -------------------------
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
