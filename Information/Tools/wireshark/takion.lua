-- initial wireshark lua dissector for takion udp communication
-- use one of 128technology/protobuf_dissector forks for working protobuf dissector

local takion = Proto("takion","Gaikai takion protocol")
local mytype = ProtoField.uint8("takion.type", "Type")
local mytype2 = ProtoField.uint8("takion.subtype", "SubType")
takion.fields = {mytype, mytype2}
function takion.dissector(buffer,pinfo,tree)
    pinfo.cols.protocol = "Takion"
    local subtree1 = tree:add(takion,buffer())
    local taktype = buffer(0,1)
    local maintype = taktype:bitfield(4,4)
    local subtype = taktype:bitfield(0,4)
    subtree1:add(mytype, taktype, maintype, "Type: " .. maintype)
    subtree1:add(mytype2, taktype, subtype, "subType: " .. subtype)
    local pos = 1

    if maintype == 2 then
        pinfo.cols.info:set("Video")
        subtree = subtree1:add(buffer(1,20),"Video")
        subtree:add(buffer(1,2),"PacketId: " .. buffer(1,2):uint())
        subtree:add(buffer(3,2),"FrameId: " .. buffer(3,2):uint())

        local framepart = buffer(5,2)
        local partcount = buffer(6,2)
        subtree:add(framepart,"partNo: " .. framepart:bitfield(0,11) .. "/" .. partcount:bitfield(3,11))
        subtree:add(partcount,"remainder2: " .. partcount:bitfield(14,2))
        subtree:add(buffer(8,2),"sth: " .. buffer(8,2))
        subtree:add(buffer(10,4),"Crypto: " .. buffer(10,4))
        local incrementer = buffer(14,4)
        subtree:add(incrementer,"TagPos: " .. incrementer)
        subtree:add(buffer(18,2),"Sync?: " .. buffer(18,4))

        pos = 21
    end

    if maintype == 3 then
        pinfo.cols.info:set("Audio")
            subtree = subtree1:add(buffer(1,18),"Audio")
            subtree:add(buffer(1,2),"PacketId: " .. buffer(1,2):uint())
            subtree:add(buffer(3,2),"NextId: " .. buffer(3,2):uint())
        subtree:add(buffer(5,5),"SomeFlags: " .. buffer(5,5))

        subtree:add(buffer(10,4),"Crypto: " .. buffer(10,4))

        local incrementer = buffer(14,4)
        subtree:add(incrementer,"TagPos: " .. incrementer)

        subtree:add(buffer(18,1),"sth: " .. buffer(18,1))

        pos = 19
    end

    if maintype == 6 then
	    pinfo.cols.info:set("Feedback (state)")
    	subtree = subtree1:add(buffer(1,7),"Feedback (State)")
    	subtree:add(buffer(1,2),"AckId: " .. buffer(1,2):uint())
    	subtree:add(buffer(3,1),"Empty: " .. buffer(3,1))
	    subtree:add(buffer(4,4),"TagPos: " .. buffer(4,4))

	    pos = 8
    end

    if maintype == 5 then
        pinfo.cols.info:set("Congestion")
        subtree = subtree1:add(buffer(1,14),"Congestion")
        subtree:add(buffer(1,2),"Empty?: " .. buffer(1,2):uint())
        subtree:add(buffer(3,2),"Queue (>> 1?): " .. buffer(3,2))
        subtree:add(buffer(5,2),"Empty " .. buffer(5,2))
        subtree:add(buffer(7,4),"Crypto: " .. buffer(7,4))
        subtree:add(buffer(11,4),"TagPos: " .. buffer(11,4))

        pos = 15
    end

    if maintype == 0 then
        pinfo.cols.info:set("Control")
        subtree = subtree1:add(buffer(1,16),"Control")
        subtree:add(buffer(1,4),"ReceiverId: " .. buffer(1,4))
        subtree:add(buffer(5,4),"Crypoto: " .. buffer(5,4))
        subtree:add(buffer(9,4),"TagPos: " .. (buffer(9,4)))
        subtree:add(buffer(13,1),"Flag1: " .. buffer(13,1))
        subtree:add(buffer(14,1),"ProtoBuffFlag: " .. buffer(14,1))
        subtree:add(buffer(15,2),"PLoad Size: " .. buffer(15,2):uint())
        if buffer(15,2):uint() > 4 then
            subtree:add(buffer(17,4),"Func Incr: " .. buffer(17,4))
            subtree:add(buffer(21,4),"class(?): " .. buffer(21,4))
        end
        if buffer(14,1):uint() == 1 then
            local rawData = buffer(26)
            local protoDiss = Dissector.get("gk.takion.proto.takionmessage")
            protoDiss:call(rawData:tvb(), pinfo, subtree1)

        end
        pos = buffer:len()
    end

    if maintype == 8 then
        pinfo.cols.info:set("Client info")
        local rawData = buffer(1)
        local protoDiss = Dissector.get("gk.takion.proto.takionmessage")
        protoDiss:call(rawData:tvb(), pinfo, subtree1)
        pos = buffer:len()
    end

    if pos < buffer:len() then
        subtree1:add(buffer(pos),"RawData (" .. buffer(pos):len() .. "b):" .. buffer(pos))
    end

end

-- load the udp.port table
udp_table = DissectorTable.get("udp.port")
-- register our protocol to handle udp port 9297 and 9296
udp_table:add(9297,takion)
udp_table:add(9296,takion)