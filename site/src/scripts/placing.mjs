export const PLACE_KEY = 'language-place';

export const PLACING_SCRIPT = `try{if(sessionStorage.getItem('${PLACE_KEY}')){document.documentElement.dataset.placing='';setTimeout(()=>delete document.documentElement.dataset.placing,1500)}}catch{}`;
