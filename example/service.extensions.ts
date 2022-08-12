import axios, { AxiosResponse, AxiosRequestConfig, AxiosInstance } from 'axios'

export class ServiceProxyBase {
  public getBaseUrl(defaultUrl: string) {
    return process.env.VUE_APP_API_URL || defaultUrl
  }
}
