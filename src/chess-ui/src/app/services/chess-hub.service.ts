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
    this.playerJoined = this.playerJoined$;

    this.gameStarted$ = new Subject<GameStarted>();
    this.gameStarted = this.gameStarted$;

    this.gameEnded$ = new Subject<GameEnded>();
    this.gameEnded = this.gameEnded$;

    this.moveMade$ = new Subject<MoveMade>();
    this.moveMade = this.moveMade$;

    this.invalidMove$ = new Subject<InvalidMove>();
    this.invalidMove = this.invalidMove$;

    this.connection.on("MoveMade", mm => this.moveMade$.next(mm));
    this.connection.on("PlayerJoined", pj => this.playerJoined$.next(pj));
    this.connection.on("GameStarted", gs => this.gameStarted$.next(gs));
    this.connection.on("GameEnded", ge => this.gameEnded$.next(ge));
    this.connection.on("InvalidMove", im => this.invalidMove$.next(im));
  }

  public async connect(username: string)
  {
    this.username = username;
    await this.connection.start();
  }

  public createGame() : string {
    var gameId = uuidv4();
    this.connection.send("CreateGame", { GameId: gameId });

    return gameId;
  }

  public joinGame(gameId: string)
  {
    this.connection.send("JoinGame", { Username: this.username, GameId: gameId })
  }

  public makeMove(gameId: string, from: string, to: string)
  {
    this.connection.send("MakeMove", { GameId: gameId, Username: this.username, From: from, To: to });
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
  GameId: string;
  Username: string;
  Player: ChessPlayerType;
}

export interface GameStarted {
  GameId: string;
  WhitePlayer: string;
  BlackPlayer: string;
}

export interface MoveMade {
  GameId: string;
  Username: string;
  From: string;
  To: string;
  Player: ChessPlayerType;
  OpponentChecked: boolean;
}

export interface GameEnded {
  GameId: string;
  Winner: ChessWinner;
  EndReason: ChessEndReason;
}

export interface InvalidMove {
  GameId: string;
  Username: string;
  From: string;
  To: string;
  Reason: string;
}