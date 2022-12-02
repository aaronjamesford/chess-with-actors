import { Injectable } from '@angular/core'
import { UntypedFormArray } from '@angular/forms';

@Injectable({
  providedIn: 'root'
})
export class NotificationsService {
    public notifications: Notification[];

    private defaultOptions = {
      autohide: true,
      class: ''
    }

    constructor() {
      this.notifications = new Array<Notification>();

      setInterval(() => {
        this.notifications.forEach(n => {
          if(n.title)
            n.timeLabel = this.getTimeLabel(n.occurredAt);
        });
      }, 10000);
    }

    public notify(title: string, body: string, options?: object) {
      this.notifications.push({
        title: title,
        body: body,
        occurredAt: new Date(),
        timeLabel: 'Just now!',
        options: options || this.defaultOptions
      });
    }

    public error(body: string) {
      this.notifications.push({
        title: "An error occurred",
        body: body,
        occurredAt: new Date(),
        timeLabel: 'Just now!',
        options: { ...this.defaultOptions, class: 'bg-danger text-light' }
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
  title?: string;
  occurredAt: Date;
  body: string;
  timeLabel?: string;
  options: any;
}