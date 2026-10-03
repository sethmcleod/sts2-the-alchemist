declare namespace App {
  interface Locals {
    /** Words and numbers in the page's language */
    l: import('./lib/lang').Lang;
    locale: import('./lib/i18n').Locale;
  }
}
