declare namespace App {
  interface Locals {
    locale: import('./lib/i18n').Locale;
    /** Words and numbers in the page's language */
    l: import('./lib/lang').Lang;
  }
}
