import { AfterViewInit, Component } from '@angular/core';
import { Router } from '@angular/router';
import { ChessHubService } from 'src/app/services/chess-hub.service';

@Component({
  selector: 'create-game',
  template: 'Creating game...',
  styleUrls: ['./header.component.css'],
})
export class CreateGameComponent implements AfterViewInit {
    constructor(private chessHub: ChessHubService, private router: Router) {}

    ngAfterViewInit(): void {
        var game = this.chessHub.createGame();
        this.router.navigate(['game', game]);
    }

}