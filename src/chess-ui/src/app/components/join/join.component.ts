import { Component } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'join-game',
  templateUrl: './join.component.html',
})
export class JoinComponent {
    constructor(private router: Router) {}

    joinGame(form: NgForm) {
        this.router.navigate(['game', form.value.gameId])
    }
}