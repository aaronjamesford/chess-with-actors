import { Component } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ChessHubService } from 'src/app/services/chess-hub.service';
import { NotificationsService } from 'src/app/services/notifications.service';

@Component({
  selector: 'chess-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {
    constructor(private chessSvc: ChessHubService, private notifs: NotificationsService) {}

    public async setUsername(form: NgForm) {
        try {
            await this.chessSvc.connect(form.value.user);
            this.notifs.notify("Connected", "Connected to chess hub");
        }
        catch(ex) {
            // TODO - Maybe something with the reset?? Should prolly not keep them disabled
            //console.log(ex);
            form.reset();
        }
    }
}
