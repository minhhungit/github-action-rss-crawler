console.log("hello world");

const urlParams = new URLSearchParams(window.location.search);
const url = urlParams.get('url');

console.log(url);

function makeid(length) {
   var result           = '';
   var characters       = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
   var charactersLength = characters.length;
   for ( var i = 0; i < length; i++ ) {
      result += characters.charAt(Math.floor(Math.random() * charactersLength));
   }
   return result;
}

(function repeatPrint(){
	setTimeout(()=>{
		$("#randomKey").text(`Random string: ${makeid(5)}`);
		repeatPrint();		
	}, 1000);
})();
