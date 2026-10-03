export const VIEW_KEY = 'item-view';
export const VIEW_SCRIPT = `try{if(localStorage.getItem('${VIEW_KEY}')==='list')document.documentElement.dataset.view='list'}catch{}`;
