# UDP protocol

The PS4 UDP protocol is a reliable UDP flow which means every message (Control messages) needs to be acknowledge by the other party.

## Control messages

The control messages are used to set up the UDP remote play protocol and for additional messages between the client and the PS4 while streaming. The first 4 init Control messages are special and not all rules apply to them. The following explanation is for control messages after the init Control messages.

### ReceiverId

The first anwer from the PS4 to the first Init Control message has a FuncIncr value which the client uses throughout the connection for its control message ReceiverId.

The first Init Control message sent from the client to the PS and its FuncIncr value is the value the PS4 will use for its ReceiverId in the control messages. (It seems to be the value is always 00004823 and not randomly generated, this could be a bug in the design of the protocol)

### Crypto (MAC)

This is the MAC which will be added to every message after the shared secret is obtained. It is 4 bytes long.

### TagPos (MAC key position)

Is used for the GMAC crypto context. It will be used to calculate the MAC in the key stream. In this project the value will always be counted up by 16 every time a new MAC will be calculated.

### Flag1 (Chunk type)

It seems to be that control messages which are sent for acknowledging data have a Flag1 value of 3, and control messages which are used for sending new data have a Flag1 value of 0.

### Protobuff Flag (Chunk flags)

If the value is 1 then it has a TakionMessage payload in it otherwise 0

### PLoad Size

Is the size of the payload and the size of the Flag1, ProtoBuffFlag, PLoadSize, FuncIncr and the class(?) value of the control message. The values of the control messages are usually 12 bytes.

### Func Incr (Sequence number)

The Func Incr value is used to to find the matching acknowledge control message to the request. The acknowledge message should have the same Func Incr value as the request. For the PS4 and the client the FuncIncr value gets increased by one by for each message sent. The client and the PS4 both have unique FuncIncr values the FuncIncr value of the client is usually 00004823 and gets increased by 1 after the BigPayload is sent.

### Class(?)

It seems to be that control messages which are sent for acknowledging data have a class value of 00019000, and control messages which are used for sending new data have a class value of 00090000.
Control messages with CorrupredFramePayload seems to have a class value of 00020000.
