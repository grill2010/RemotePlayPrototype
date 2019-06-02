var RPCTRLWRAPPER_LIB = "RpCtrlWrapper.dll";
var libSmartGlassCoreBaseAddr = 0;
var function1Addr = 0x1C7A0;
var cryptInit = 0xA4E30;
var cryptCalculateSha256 = 0xA5F70;
var HashData = 0x227000;
var vsnprintfsAddr = 0x210EA2;
var libRpCtrlWrapperBaseAddr = Module.findBaseAddress(RPCTRLWRAPPER_LIB);

function bytesToString(bytes) {
	var array = new Uint8Array(bytes);
	var str = "";
	for (var i=0; i<array.length; i++){
		var tmp = array[i].toString(16);
		if (tmp.length < 2){
			tmp = "0" + tmp;
		}
		str += tmp;
	}
	return str;
}

var vsnprintfs = libRpCtrlWrapperBaseAddr.add(vsnprintfsAddr);
Interceptor.attach(vsnprintfs, {
	onEnter: function(args){
		this.dst = args[0];
		this.size = args[1].toInt32();
	},
	onLeave: function(retval){
		var str = Memory.readUtf8String(this.dst);
		console.log("[LOG]" + str);
	}
});

Interceptor.attach(libRpCtrlWrapperBaseAddr.add(cryptInit), {
	onEnter: function(args){
		this.cls = args[0];
		console.log("Crypt::init called ");
	},
	onLeave: function(retval){
	}
});

Interceptor.attach(libRpCtrlWrapperBaseAddr.add(cryptCalculateSha256), {
	onEnter: function(args){
		this.cls = args[0];
		this.inputBuffer = args[1];
		this.inputSize = args[2].toInt32();
		this.outputPtr = args[3];
		console.log("Crypt::CalculateSha256 called ");
	},
	onLeave: function(retval){
		//var data = Memory.readByteArray(this.inputBuffer, this.inputSize);
		var hash = Memory.readByteArray(this.outputPtr, 32);
		//console.log("InputBuffer: " + bytesToString(data));
		//console.log("SHA256: " + bytesToString(hash));
	}
});


Interceptor.attach(libRpCtrlWrapperBaseAddr.add(HashData), {
	onEnter: function(args){
		this.cls = args[0];
		console.log("CryptHashData called ");
	},
	onLeave: function(retval){
	}
});