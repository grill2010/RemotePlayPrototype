# Wireshark Protobuf Dissector
A Wireshark Lua plugin to decode/dissect Google Protobuf messages like the one which are sent by the PS4 Remote Play client.

This dissector was originally taken from [here](https://github.com/128technology/protobuf_dissector). It was slightly modified to make it work with
the Wireshark 2.6.4. It should also work with the newest version but was not tested.

# Usage
Just copy this entire directory in your personal plugin folder of wireshark. You can figure out where your personal plugin folder is located by going to
Help, About Wireshark and then click the Folders tab.There should be an entry which is called "Personal Plugins".
