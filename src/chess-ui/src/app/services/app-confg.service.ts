import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { firstValueFrom } from 'rxjs'

export interface AppConfig {
    apiUrl: string;
}

@Injectable()
export class AppConfigService
{
    private config: AppConfig | undefined;
    private defaultConfig: AppConfig;

    constructor(private httpClient: HttpClient) {
        this.defaultConfig = {
            apiUrl: "http://localhost:5184"
        };
    }

    public async loadConfig() {
        this.config = await firstValueFrom(this.httpClient.get<AppConfig>('/assets/app.config.json'));
    }

    public getConfig() : AppConfig {
        return this.config || this.defaultConfig;
    }
}