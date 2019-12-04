export class ServiceProxyBase {
    public getBaseUrl(defaultUrl: string) {
        return window['config']['VUE_APP_API_URL'] || defaultUrl;
    }
}