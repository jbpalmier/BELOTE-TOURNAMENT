import { Routes } from '@angular/router';
import { TeamsPageComponent } from './features/teams/teams-page.component';
import { SchedulePageComponent } from './features/schedule/schedule-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'teams' },
  { path: 'teams', component: TeamsPageComponent },
  { path: 'schedule', component: SchedulePageComponent },
];
