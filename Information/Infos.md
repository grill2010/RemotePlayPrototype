# UDP protocol

The ps4 udp protocol is a reliable udp flow which means every message (Control messages) needs to be acknowledge by the other party.

## Control messages

The control messages are used to set up the udp remote play protocol and for additional messages between client and PS4 while streaming. The first 4 init Control messages are special and not all rules apply to them. The following exlanation is for control messages after the Init Control messages.

### ReceiverId

The first anwer of from the PS4 to the first Init Control message has a FuncIncr value which the client uses throughout the connection for its control message ReceiverId.

The first Init Control message sent from the client to the PS and its FuncIncr value is the value the PS4 will use for its ReceiverId in the control messages. (It seems to be the value is always 00004823)

### Crypoto

That's not clear yet what this value is used for, but it could be some random number which is then used for the IV counter.

### TagPos

Not yet clear. Seems to be used when the crypoto value is used. TagPos is contained in other messages as well.

### Flag1

It seems to be that control messages which are sent for acknowleding data have a Flag1 value of 3, and control messages which are used for sending new data have a Flag1 value of 0.

### Protobuff Flag
If the value is 1 then it has a TakionMessage payload in it otherwhise 0

### PLoad Size

Is the size of the payload and the size of the Flag1, ProtoBuffFlag, PLoadSize, FuncIncr and the class(?) value of the control message. The values of the control messages are usually 12 bytes.

### Func Incr

The Func Incr value is used to to find the matching acknowledge control message to the request. The acknwoledge message should have the same Func Incr value as the request. For the PS4 and the client the FuncIncr value gets increased by one by for each message sent. The client and the PS4 both have unique FuncIncr values the FuncIncr value of the client is usally 00004823 and gets increased by one after the BigPayload is sent.

### Class(?)

It seems to be that control messages which are sent for acknowleding data have a class value of 00019000, and control messages which are used for sending new data have a class value of 00090000.
Control messages with CorrupredFramePayload seems to have a class value of 00020000.