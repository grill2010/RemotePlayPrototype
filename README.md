> Disclaimer: PS4, PlayStation 4, Dualshock and PSN are trademarks of Sony Computer Entertainment Inc. PSJoy is in no way endorsed by or affiliated with Sony Computer Entertainment Inc, or any associated subsidiaries, logos or trademarks.

# RemotePlayPrototype info

Quick and dirty prototype to play around with the protocol.
This is just a research project there is no support. If there are some enthusiastic reverse engineers out there it would be great if they could contribute to the project.

It was not implemented only by my own, we are a group of developers who are interested in making the remote play protocol open source. The base of the project was taken from https://github.com/grill2010/ps4-remote-play but it seems that the initial creator has unfortunately abandoned the project. I'm not a professional reverse engineer so in case you have some suggestions feel free to let us know.

And again, this is prototype code, it is ugly, and it could contain bugs.

# Current status

The prototype is able to register with the PS4, it can perform the initial TCP handshake with the console and it can receive the UDP stream data, but it will stop after a few seconds as the current implemented streaming protocol doesn't correctly use [GMAC](https://en.wikipedia.org/wiki/Galois/Counter_Mode). Unlike many other streaming services Sony also like to encrypt their audio and video frames so you can't just process them unfortunately.

# General information

The registration and the initial handshake are performed via REST

**Registration:**

- /sce/rp/regist

Please see class *PS4RegistrationService* method *PairConsole*.
The registration uses a own AES crypto context. You can see how this is done in class *CryptoService* method *GetRegistryAesKeyForPin*.
The pin is the number which you can obtain like [this](https://manuals.playstation.net/document/en/ps4/settings/adddevice.html).

(If the link isn't working -> On your PS4 select [Settings] > [Remote Play Connection Settings] > [Add Device])

**Connection:**

- /sce/rp/session
- /sce/rp/session/ctrl

Please see class *PS4ConnectionService* method *HandleSessionRequest* and *HandleControlRequest*.
The initial connection handshake uses another AES crypto context. You can see how this is done in class *CryptoService* method *GetSessionAesKeyForControl*. The rpKey is obtained from the registration process and the rpNonce is obtained from the response header of the */sce/rp/session* GET request.

After the */sce/rp/session* a ping pong thread is started in order to answer the keep-alive messages. See *PingPongHandler*in class *PS4ConnectionService*.

The UDP protocol basically uses protobuf protocol from Google. The protobuf metadata were extracted with [PROTOD](https://github.com/Manouchehri/Protod). We used the Android libremote.so file and the RpCtrlWrapper.dll to extract this information. The corresponding protobuf classes can be found in *RemotePlayPrototype/Ps4RemotePlayPrototype/Ps4RemotePlay/Protocol/Message/*. The libremote.so file is normally obfuscated when you just try to extract it from the newest RemotePlay apk. The obfuscation was removed so that it can be used for further investigations.

More information about the UDP protocol can be found in the *Information* directory.

# What is missing

The UDP stream initialization uses some ECDH mechanism (curve algo is Secp256k1) although we managed to figure out how the private and public key pair is generated and even though the PS4 is sending us the public key from the console, we do not know how the resulting shared secret is used. Probably hashed and salted and used for another AES context to decrypt the audio and video frames.

On top of that the streaming protocol also uses some [GMAC](https://en.wikipedia.org/wiki/Galois/Counter_Mode) mechanism to be sure the messages were not modified, but we do not know how this is done and how this should be implemented.

The PS4 also uses some FEC correction mechanism but because of the other problems we didn't check that. The FEC correction is needed to recover lost frame packets and to avoid fragments in the stream.

# Which tools did you use?

- Wireshark
- IDA
- Apktool (for reversing the Android client and obtaining libremote.so file)
- [Frida](https://www.frida.re/docs/functions/)
- [API Monitor v2](http://www.rohitab.com/apimonitor)
- Microsoft Visual Studio Community 2017
