import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HTTP_INTERCEPTORS} from '@angular/common/http';
import { provideHttpClient, withInterceptorsFromDi  } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { JwtInteceptor } from './security/jwt.interceptor';
import { routes } from './app.routes';
import { provideNativeDateAdapter } from '@angular/material/core';
import { importProvidersFrom } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    {provide: HTTP_INTERCEPTORS, useClass: JwtInteceptor, multi: true},
    provideHttpClient(withInterceptorsFromDi()), provideAnimations(),
    provideNativeDateAdapter(),
    provideHttpClient(),
     importProvidersFrom(ReactiveFormsModule)
  ]
};
