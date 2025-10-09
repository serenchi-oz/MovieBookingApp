import { enableProdMode, importProvidersFrom, isDevMode } from '@angular/core';
import { BrowserModule, bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import {
  provideRouter,
  withInMemoryScrolling,
  withPreloading,
  PreloadAllModules,
} from '@angular/router';
import { provideStore, provideState } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideRouterStore, routerReducer } from '@ngrx/router-store';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { AppComponent } from './app/app.component';
import { environment } from './environments/environment';
import { APP_ROUTES } from './app/routes/app.routes';

// Interceptors
import { ApiInterceptor } from './app/interceptors/api.interceptor';
import { ErrorInterceptor } from './app/interceptors/error.interceptor';

// State Management
import { GenreEffects } from './app/state/effects/genre.effects';
import { MovieEffects } from './app/state/effects/movie.effects';
import { AuthEffects } from './app/state/effects/auth.effects';
import { WatchlistEffects } from './app/state/effects/watchlist.effects';

import { genreReducer, GENRE_FEATURE_KEY } from './app/state/reducers/genre.reducer';
import { movieReducer, MOVIE_FEATURE_KEY } from './app/state/reducers/movie.reducer';
import { authReducer, AUTH_FEATURE_KEY } from './app/state/reducers/auth.reducers';
import { watchlistReducer, WATCHLIST_FEATURE_KEY } from './app/state/reducers/watchlist.reducers';
import { ROUTER_FEATURE_KEY } from './app/state/selectors/router.selectors';

// Enable production mode
if (environment.production) {
  enableProdMode();
}

// Bootstrap application
bootstrapApplication(AppComponent, {
  providers: [
    importProvidersFrom(BrowserModule),

    // ⚡ Optimized HTTP client (supports Fetch API for better performance)
    provideHttpClient(
      withInterceptors([ApiInterceptor, ErrorInterceptor]),
      withFetch()
    ),

    provideAnimations(),

    // 🚦 Router configuration
    provideRouter(
      APP_ROUTES,
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
      withPreloading(PreloadAllModules)
    ),

    // 🧠 NgRx Store & Effects setup
    provideStore({
      [ROUTER_FEATURE_KEY]: routerReducer,
    }),

    provideEffects(GenreEffects, MovieEffects, AuthEffects, WatchlistEffects),

    provideState(GENRE_FEATURE_KEY, genreReducer),
    provideState(MOVIE_FEATURE_KEY, movieReducer),
    provideState(AUTH_FEATURE_KEY, authReducer),
    provideState(WATCHLIST_FEATURE_KEY, watchlistReducer),

    provideRouterStore(),

    // 🛠️ DevTools (active only in dev mode)
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),
      autoPause: true,
      trace: false,
      connectInZone: true,
    }),
  ],
}).catch((err) => console.error('❌ Bootstrap error:', err));
