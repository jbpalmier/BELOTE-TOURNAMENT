import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

export interface TeamDto {
  id: string;
  name: string;
}

export interface ScheduleResponse {
  rounds: Array<{
    round: number;
    matches: Array<{
      table: number;
      teamA: { id: string; name: string };
      teamB: { id: string; name: string };
    }>;
  }>;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Teams
  getTeams(): Observable<TeamDto[]> {
    return this.http.get<TeamDto[]>(`${this.baseUrl}/api/teams`);
  }

  createTeam(payload: { name: string }): Observable<TeamDto> {
    return this.http.post<TeamDto>(`${this.baseUrl}/api/teams`, payload);
  }

  updateTeam(id: string, payload: { name: string }): Observable<TeamDto> {
    return this.http.put<TeamDto>(`${this.baseUrl}/api/teams/${id}`, payload);
  }

  deleteTeam(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/teams/${id}`);
  }

  // Schedule
  generateSchedule(): Observable<ScheduleResponse> {
    return this.http.post<ScheduleResponse>(`${this.baseUrl}/api/schedule/generate`, {});
  }

  downloadSchedulePdf(payload: any) {
    return this.http.post(`${this.baseUrl}/api/schedule/pdf`, payload, { responseType: 'blob' });
  }

  deleteAllTeams() {
    return this.http.delete<void>(`${this.baseUrl}/api/teams`);
  }
}
