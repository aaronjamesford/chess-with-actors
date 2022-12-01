import { Injectable } from '@angular/core'

@Injectable({
  providedIn: 'root'
})
export class NotificationsService {
    public notifications: Notification[];

    constructor() {
      this.notifications = new Array<Notification>();

      setInterval(() => {
        this.notifications.forEach(n => {
          n.timeLabel = this.getTimeLabel(n.occurredAt);
        });
      }, 10000);
    }

    public notify(title: string, body: string, delay?: number) {
      this.notifications.push({
        title: title,
        body: body,
        delay: delay,
        occurredAt: new Date(),
        timeLabel: 'Just now!'
      });
    }

    public dismiss(notif: Notification) {
      this.notifications = this.notifications.filter(n => n != notif);
    }

    private getTimeLabel(when: Date) {
      var diff = new Date(new Date().valueOf() - when.valueOf());

      diff.setHours(diff.getHours() - 1); // something in the date is fucked... plz send help

      if(diff.getHours() == 1)
        return "An hour ago";
      if(diff.getHours() > 1)
        return diff.getHours() + " hours ago";

      if(diff.getMinutes() == 1)
        return "A minute ago";
      if(diff.getMinutes() > 1)
        return diff.getMinutes() + " minutes ago";

      if(diff.getSeconds() >= 10)
        return diff.getSeconds() + " seconds ago";

      return "A few seconds ago";
    }
}

export interface Notification {
  title: string;
  occurredAt: Date;
  body: string;
  delay?: number;
  timeLabel: string;
}