import { Component } from '@angular/core'
import { NotificationsService } from 'src/app/services/notifications.service';

@Component({
    selector: "notification-hub",
    styleUrls: ['./notifications.component.css'],
    templateUrl: './notifications.component.html'
})
export class NotificationsComponent {
    public notifs: NotificationsService;

    constructor(svc: NotificationsService) {
        this.notifs = svc;
    }
}