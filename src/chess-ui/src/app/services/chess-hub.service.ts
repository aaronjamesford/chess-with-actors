import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
import { Observable, Subject } from 'rxjs';
import { v4 as uuidv4 } from 'uuid';
import { AppConfigService } from './app-confg.service';

@Injectable({
  providedIn: 'root'
})
export class ChessHubService {

  public moveMade : Observable<MoveMade>;
  public playerJoined : Observable<PlayerJoined>;
  public gameStarted : Observable<GameStarted>;
  public gameEnded : Observable<GameEnded>;
  public invalidMove : Observable<InvalidMove>;

  private moveMade$ : Subject<MoveMade>;
  private playerJoined$ : Subject<PlayerJoined>;
  private gameStarted$ : Subject<GameStarted>;
  private gameEnded$ : Subject<GameEnded>;
  private invalidMove$ : Subject<InvalidMove>;

  private connection : HubConnection;
  public username?: string;

  constructor(configSvc: AppConfigService) {

    this.connection = new HubConnectionBuilder()
      .withUrl(configSvc.getConfig().apiUrl + "/hubs/chess")
      .build();

    this.playerJoined$ = new Subject<PlayerJoined>();
    this.playerJoined = this.playerJoined$.asObservable();

    this.gameStarted$ = new Subject<GameStarted>();
    this.gameStarted = this.gameStarted$.asObservable();

    this.gameEnded$ = new Subject<GameEnded>();
    this.gameEnded = this.gameEnded$.asObservable();

    this.moveMade$ = new Subject<MoveMade>();
    this.moveMade = this.moveMade$.asObservable();

    this.invalidMove$ = new Subject<InvalidMove>();
    this.invalidMove = this.invalidMove$.asObservable();

    this.connection.on("MoveMade", (mm: MoveMade) => {
      console.log(mm);
      this.moveMade$.next(mm);
    });
    this.connection.on("PlayerJoined", (pj: PlayerJoined) => {
      console.log(pj);
      this.playerJoined$.next(pj);
    });
    this.connection.on("GameStarted", (gs: GameStarted) => {
      console.log(gs);
      this.gameStarted$.next(gs);
    });
    this.connection.on("GameEnded", (ge: GameEnded) => {
      console.log(ge);
      this.gameEnded$.next(ge);
    });
    this.connection.on("InvalidMove", (im: InvalidMove) => {
      console.log(im);
      this.invalidMove$.next(im);
    });
  }

  public async connect(username: string)
  {
    this.username = username;
    await this.connection.start();
  }

  public createGame() : string {
    var gameId = uuidv4();
    this.connection.send("CreateGame", gameId);

    return gameId;
  }

  public joinGame(gameId: string)
  {
    this.connection.send("JoinGame", this.username, gameId )
  }

  public makeMove(gameId: string, from: string, to: string)
  {
    this.connection.send("MakeMove", this.username, gameId, from, to);
  }

}

export enum ChessPlayerType {
  White, Black
}

export enum ChessWinner {
    WhiteWin, BlackWin, Draw
}

export enum ChessEndReason {
    Checkmate, Stalemate, Abandoned, Resigned
}
 
export interface PlayerJoined {
  gameId: string;
  username: string;
  player: ChessPlayerType;
}

export interface GameStarted {
  gameId: string;
  whitePlayer: string;
  blackPlayer: string;
}

export interface MoveMade {
  gameId: string;
  username: string;
  from: string;
  to: string;
  player: ChessPlayerType;
  opponentChecked: boolean;
}

export interface GameEnded {
  gameId: string;
  winner: ChessWinner;
  endReason: ChessEndReason;
}

export interface InvalidMove {
  gameId: string;
  username: string;
  from: string;
  to: string;
  reason: string;
}