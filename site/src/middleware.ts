import { defineMiddleware } from 'astro:middleware';
import { language } from './lib/content';
import { localeOf } from './lib/i18n';

export const onRequest = defineMiddleware((context, next) => {
  const locale = localeOf(context.url.pathname);
  context.locals.locale = locale;
  context.locals.l = language(locale);
  return next();
});
