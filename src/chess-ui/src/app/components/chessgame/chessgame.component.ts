import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { NgxChessBoardView } from 'ngx-chess-board';
import { filter } from 'rxjs';
import { ChessHubService, ChessPlayerType } from 'src/app/services/chess-hub.service';
import { NotificationsService } from 'src/app/services/notifications.service';

@Component({
  selector: 'chess-gane',
  templateUrl: './chessgame.component.html',
})
export class ChessGameComponent implements AfterViewInit {
    @ViewChild('board', {static: false}) private board!: NgxChessBoardView;

    private id?: string;
    
    isWhite: boolean = true;
    started: boolean = true;

    constructor(activatedRoute: ActivatedRoute, private chessService: ChessHubService, private notifs: NotificationsService) {
        activatedRoute.params.subscribe(p => {
            this.id = p['id'];
            console.log(this.id);
        });

        this.chessService.playerJoined
            .pipe(filter(pj => pj.username == this.chessService.username && pj.gameId == this.id))
            .subscribe(pj => {
                if(pj.player == ChessPlayerType.Black)
                {
                    this.board.reverse();
                    this.isWhite = false;
                }
            });

        this.chessService.playerJoined
            .pipe(filter(pj => pj.username != this.chessService.username && pj.gameId == this.id))
            .subscribe(pj => {
                this.notifs.notify('Player joined!', '${pj.username} joined the game!');
            });

        this.chessService.gameStarted
            .pipe(filter(gs =>gs.gameId == this.id))
            .subscribe(_ => this.started = true);

        this.chessService.moveMade
            .pipe(filter(mm => mm.gameId == this.id && mm.username != this.chessService.username))
            .subscribe(mm => {
                this.board.move(mm.from + mm.to);
            });

        this.chessService.invalidMove
            .pipe(filter(im => im.gameId == this.id && im.username == this.chessService.username))
            .subscribe(im => {
                var message = 'Unable to make move ${im.from} to ${im.to} because: ${im.reason}';
                this.notifs.error(message);
                console.log(message);
            })
    }

    ngAfterViewInit(): void {
        if(this.id)
            this.chessService.joinGame(this.id);
    }

    moveChange() {
        var move = this.board.getMoveHistory().at(-1);
        if(this.isWhite && move?.color == 'black')
            return;

        if(!this.isWhite && move?.color == 'white')
            return;

        var from = move!.move.substring(0, 2);
        var to = move!.move.substring(2);

        this.chessService.makeMove(this.id!, from, to);
    }
}
