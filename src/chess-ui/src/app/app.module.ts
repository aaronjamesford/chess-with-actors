import { NgModule, APP_INITIALIZER } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Routes, RouterModule } from '@angular/router'

import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { NgxChessBoardModule } from "ngx-chess-board";

import { ChessHubService } from './services/chess-hub.service';
import { AppConfigService } from './services/app-confg.service';

import { AppComponent } from './app.component';
import { HeaderComponent } from './components/header/header.component';
import { HomeComponent } from './components/home/home.component';
import { NotificationsService } from './services/notifications.service';
import { NotificationsComponent } from './components/notifications/notifications.component';
import { ChessGameComponent } from './components/chessgame/chessgame.component';
import { CreateGameComponent } from './components/create/create.component';
import { JoinComponent } from './components/join/join.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'create', component: CreateGameComponent },
  { path: 'join', component: JoinComponent },
  { path: 'game/:id', component: ChessGameComponent }
];

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    HomeComponent,
    NotificationsComponent,
    ChessGameComponent,
    CreateGameComponent,
    JoinComponent,
  ],
  imports: [
    FormsModule,
    ReactiveFormsModule,
    BrowserModule,
    NgbModule,
    HttpClientModule,
    RouterModule.forRoot(routes),
    NgxChessBoardModule.forRoot()
  ],
  exports: [RouterModule],
  providers: [
    ChessHubService,
    NotificationsService,
    AppConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: initConfig,
      deps: [AppConfigService],
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }

export function initConfig(configSvc: AppConfigService) {
  return async () => await configSvc.loadConfig();
}