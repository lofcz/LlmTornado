// See https://aka.ms/new-console-template for more information
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Chat;

var api = new TornadoApi([
                new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                new ProviderAuthentication(LLmProviders.Anthropic, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                ]);

ContextContainer contextContainer = new ContextContainer();
# region Chat Messages
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.System,
    "You are a helpful AI assistant that can help with travel planning, weather information, and general questions."
));
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    @"(GOSS NET 1) Tape 18/1 Page 80

01 02 38 29 CDR
Houston, Apollo 11. Could you give us a couple of high-gain antenna angles, please?

01 02 38 35 CC
Roger. Stand by, 11.

01 02 38 44 CC
Roger, 11. Pitch minus 35, yaw 0. Over.

01 02 38 5O CDR
Thank you.

01 02 39 03 CDR
We got them.

01 02 39 05 CC
Okay.

01 02 39 28 CDR
Houston, Apollo 11. How do you read high gain?

01 02 39 30 CC
Read you loud and clear on high gain down here, and everything's looking good from our standpoint for your burn. Over.

01 02 39 36 CDR
Okay, Bruce.

01 02 45 38 CDR
Houston, burn completed. You copying our residuals?

01 02 45 40 CC
That's affirmative.

01 02 46 00 CDR
And, Houston, looked like we saw about 87 or 88 psi on chamber pressure that time. I'd like you to look at that on the ground.

01 02 46 13 CC
Roger, 11. We'll take a look at that and get back in a few minutes.

01 02 46 38 CC
11, Houston. On our real-time telemetry we saw 95 to 97 psi on chamber pressure. We'll - We will look at the recordings down here, thought, and get back with you again. Over.

01 02 46 41 CDR
Okay. Thank you.

01 02 46 50 CC
And we've copied your residuals, 11.

01 02 46 56 CDR
Roger. No, we're not going to trim those ...

01 02 49 51 CC
11, Houston. Could we get your DELTA-V counter reading, please, from this burn?

01 02 50 00 LMP
That's minus 3.8.

01 02 50 02 CC
Minus 2.8?

01 02 50 04 LMP
3.8.



(GOSS NET 1) Tape 18/2 Page 81

01 02 50 06 CC
Roger. Copy minus 3.8.

01 02 51 04 CDR
Houston, is there anything else you need on the burn status report?

01 02 51 15 CC
This is Houston. Negative, 11.

01 02 51 23 CDR
Roger. Thank you.

01 02 54 41 CC
Apollo 11, this is Houston. Over.

01 02 54 45 CMP
Roger. Apollo 11.

01 02 54 47 CC
Roger. I just wanted to remind you that we haven't noticed on the TM the VERB 66 after the burn. And for your information, we played the recorded TV back last night, I believe, after you all turned in for your rest period, and the pictures came out quite well. Over.

01 02 55 10 CMP
Did you get any usable pictures out of MILA on that first pass?

01 02 55 15 CC
Not that we've seen. We had word on the voice loop that MILA reported that they had gotten a minute's worth of TV signal, and Goldstone reported that they had gotten about a minute's worth of modulation but that they weren't able to get anything off of it.

01 02 55 35 CMP
0kay. Thanks.

01 02 55 40 CC
Okay. We have another input here, 11, that the MILA data was recognizable as a picture, but we don't have any evaluation as to the quality of the picture. Over.

01 02 55 53 CMP
Okay.

01 02 55 55 CC
And for our information, we've been watching a PCO2 again. Did you change a lithium hydroxide canister this morning? Over.

01 02 56 08 CDR
Yes. We did, and we've been seeing 1.7 percent in the spacecraft ever since.

01 02 56 14 CC
Roger. That agrees with our data.

01 02 56 19 CDR
1.7 millimeters.

01 02 56 21 CC
Roger. We copy.



(GOSS NET 1) Tape 18/3 Page 82

01 02 57 00 CDR
Houston, Apollo 11. We're starting our maneuver to PTC attitude.

01 02 57 05 CC
Roger.

01 02 59 21 CC
11, this is Houston. Over.

01 02 59 26 LMP
Go ahead.

01 02 59 27 CC
Roger. From a propellant-balancing standpoint, we recommend that you use quads Alfa and Bravo to start the PTC maneuvers. Over.

01 02 59 39 LMP
Roger. Understand Alfa and Bravo.

01 02 59 41 CC
Roger. Out.

01 03 06 14 CC
11, this is Houston. Over.

01 03 06 19 CDR
Go ahead, Houston.

01 03 06 21 CC
Roger. For CRYO-balancing purposes, we'd like you to turn the heater and oxygen tank number 1 off at this time. Over.

01 03 06 32 CDR
Okay. Stand by.

01 03 06 35 CC
Everything else in the CRYO system remains the same.

01 03 06 39 CDR
0kay.

01 03 06 56 CMP
Okay. We have O2 heater tank 1 off.

01 03 07 01 CC
Houston. Roger. Out.

01 03 07 10 CMP
How is EECOMM today? Is he happy with all those good things?

01 03 07 14 CC
Oh, EECOMM is happy, and after you get PTC set up, we've got a little procedure from EEC0MM here to check out the O2 flow and the O2 flow sensor in your cabin enrichment. Over.

01 03 07 29 CMP
Okay.

01 03 07 35 CMP
It'll be a while, Bruce. We're just now arriving in PTC attitude, and we're going to our 20 minutes of monitoring thruster activity.

01 03 07 42 CC
Roger. We copy. He'll be here."
));
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Let me look into that.."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Hi! I'm planning a trip to Tokyo next month. What's the weather like there in November?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Hello! Tokyo in November is lovely. The weather is generally mild with temperatures ranging from 10-17°C (50-63°F). It's one of the best times to visit as the autumn foliage is beautiful. You'll want to bring a light jacket and layers."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    @"
01 03 13 03 CMP
Houston, Apollo 11. Are you going to take control of the 0MNI's now and switch us between B and D?

01 03 13 08 CC
This is Houston. Stand by one.

01 03 13 35 CC
11, Houston.

01 03 13 38 CMP
Go ahead.

01 03 13 40 CC
Mike, how about selecting Bravo at this time, and I'll give you a COMP configuration here, shortly.

01 03 13 49 CMP
Okay.

01 03 14 24 CMP
That PTC sure worked well last night.

01 03 14 28 CC
Outstanding.

01 03 14 55 CC
11, Houston.

01 03 15 00 CMP
Go ahead.

01 03 15 02 CC
Okay. Roll for the COMM situation: have S-band antenna OMNI A in Bravo, S-band antenna OMNI to OMNI, high-gain track to MANUAL, and the pitch is minus 50 and yaw is 270.

01 03 15 24 CMP
You may have to repeat some of that, James. We've got a LM guy taking care of the high gain right now.

01 03 15 30 CDR
Yes, and he is eyeballing the Earth.

01 03 15 32 CMP
He's got his head out the window.

01 03 15 35 CC
I understand, I had trouble on 12 with him, too.

01 03 15 42 LMP
Say again what you'd like.

01 03 15 44 CC
Okay, The S-band antenna OMNI A switch to Bravo which you have now, and S-band antenna OMNI to the 0MNI position, and the high-gain track to the MANUAL position, and the pitch and yaw angles are minus 50 for pitch and yaw is 270.

01 03 16 10 LMP
Minus 50 and 270.

01 03 17 09 LMP
Hey, Jim, I'm looking through the monocular now, and to coin an expression, the view is just beautiful. It's out of this world. I can see all the islands in the Mediterranean. Some larger and smaller islands of Majorca, Sardinia, and Corsica.



(GOSS NET 1) Tape 18/5 Page 84

A little haze over the upper Italian peninsula, some cumulus clouds out over Greece. The Sun is setting on the eastern Mediterranean now. The British Isles are definitely greener in color than the brownish green that we have in the islands, in the peninsula of Spain. Over.

01 03 18 03 CC
Roger. I understand that the Northern Africa - Mediterranean area is fairly clear today, huh?

01 03 18 12 LMP
Right.

01 03 18 14 CDR
Yes. We see a bunch of roads with cars driving up and down, too.

01 03 18 19 CC
Do you find that the monocular is any good to you, Buzz?

01 03 18 27 LMP
Yes, It would be nicer if it had another order of magnitude of power on it. Of course, it has a tendency to jiggle around a little bit, and you might want to have some sort of a bracket. I hate to use that word though.

01 03 18 53 LMP
Got an anticyclone going in the southern hemisphere southeast of Brazil, and some - Well, the diameter of it must be over 2000 miles across.

01 03 19 15 CC
How does the weather look up in the southern part of the western hemisphere, or up in the United States area?

01 03 19 26 LMP
Well, you all are just beginning to come over the limb now. I can see parts of Central America, and it looks to be fairly clear there. The islands in the Caribbean are beginning to come in and rather a few streaming lines of clouds. Looks like there is a system up to the - well, off of Greenland that has some large cloud streamers extending back down to the southwest. The east coast of the U.S. is just coming into view now, and it doesn't look too bad that I can see right now. We may have some pretty good shots later on this afternoon. Over.

01 03 20 16 CC
Roger. Thank you.

01 03 21 50 LMP
Houston, Apollo 11.

01 03 21 54 CC
Go ahead, 11.



(GOSS NET 1) Tape 18/6 Page 85

01 03 21 56 LMP
I've got a comment about the point on the Earth where the Sun's rays reflect back up toward us. In general, the color of the oceans is mostly uniform and it's bright and darker blue except for that region that's about one-eighth of an Earth's radius in diameter; and in this circular area, the blue of the water turns a grayish color, and I'm sure that's where the Sun's rays are being reflected back on up toward us. Over.

01 03 22 41 CC
Roger, Buzz. We noticed the same thing. It's very similar to looking at a light shining on something like a billiard ball or a bowling ball. You get this bright spot in the blue of the water, and that turns it to sort of a grayish color.

01 03 22 57 LMP
Yes. Is there a Navy term for that?

01 03 23 00 CC
(Laughing.) A lot of gray paint.

01 03 25 27 CC
11, Houston.

01 03 25 32 CMP
Go ahead, Houston.

01 03 25 38 CC
Mike, are you satisfied with P23 now?

01 03 25 43 CMP
Yes, I'm happy with the last updates we got, you know, in terms of what it did to our state vector. Still not altogether happy with the various procedures. If we could pick stars within the smaller range of trunnion angles so that you could allow P23 to pick its own maneuver and go to that substellar point and then have that star visible, that would seem to me to be the simplest and best way to do it.

01 03 26 14 CC
How about the - -

01 03 26 19 LMP
... correction on that last ...

01 03 26 20 CC
How about the horizon now? Is it pretty well defined for you and no longer hazy?

01 03 26 24 CMP
Yes. We're far enough out now that the - I think the horizon definition variation is lost in the North.

01 03 26 57 LMP
Hey, Jim.

01 03 27 03 CC
Go ahead, Buzz.



(GOSS NET 1) Tape 18/7 Page 86

01 03 27 06 LMP
Looks like the best way to get a steady view through the monocular is just steady it out and let it close when it's in front of your eye, and then you kind of float up next to it so that you're not touching it at all. It has a very slow drift, and you get a better - steadier view that way.

01 03 27 25 CC
Sounds good.

01 03 27 47 CC
How does it feel to be airborne again, Buzz?

01 03 27 51 LMP
Well, I'll tell you, I've been having a ball floating around inside here, back and forth up to one place and back to another. It's like being outside, except more comfortable.

01 03 28 04 CC
It's a lot bigger than our last vehicle.

01 03 28 06 CMP
... kept me busy, I'm ...

01 03 28 10 CMP
Say again about Buzz -

01 03 28 11 LMP
Yes. It sure is nice in here.

01 03 28 13 CC
I said it's a lot bigger than the last vehicle that Buzz and I were in.

01 03 28 17 CMP
Oh, yes. It's been nice. I've been very busy so far. I'm looking forward to taking the afternoon off. I've been cooking, and sweeping, and almost sewing, and you know, the usual little housekeeping things.

01 03 28 30 CC
It was very convenient the way they put the food preparation system right next to the NAV station.

01 03 28 43 CDR
Everything is right next to everything in this vehicle.

01 03 28 48 LMP
Not if you're in the ...

01 03 28 53 CDR
Jim, it's been a little warm in the machine throughout yesterday and last night during the PTC. It cooled off somewhat with the windows buttoned up, and we've seen suit temperatures of about - the high 40's and cabin temperatures in the low 60's. But this seems to be still a little bit on the warm side.

01 03 29 28 CC
I understand that it got a little warm during the day and cooled down a little bit when you put the shades up, but you're still a little bit warm. Do you have any moisture condensation or anything like that on the wall?
"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    @"Absolutely! Here are some top recommendations:\n1. Senso-ji Temple in Asakusa\n2. Tokyo Skytree for panoramic views\n3. Meiji Shrine in Shibuya\n4. Tsukiji Outer Market for food\n5. TeamLab Borderless digital art museum\n6. Imperial Palace East Gardens\n\nWould you like more specific information about any of these?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "The TeamLab museum sounds interesting. How much does it cost?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "TeamLab Borderless tickets are typically around ¥3,200 (about $22 USD) for adults. I'd recommend booking in advance as it's very popular. The museum is in Odaiba and takes about 2-3 hours to explore fully."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Great! Oh, I almost forgot - I need to book a hotel too. Any suggestions for good areas to stay?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "For first-time visitors, I'd recommend staying in:\n1. Shinjuku - great transport hub, lots of restaurants\n2. Shibuya - trendy area, excellent shopping\n3. Asakusa - traditional atmosphere, more affordable\n4. Ginza - upscale, central location\n\nWhat's your budget range per night?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I'm thinking around $150-200 per night. Shinjuku sounds good for the transport connections."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Excellent choice! In that budget range, look at hotels like Hotel Gracery Shinjuku, Keio Plaza Hotel, or Century Southern Tower. They're all well-located near Shinjuku Station with good amenities."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Perfect. Now about food - I'm vegetarian. Is that difficult in Tokyo?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Tokyo has become much more vegetarian-friendly in recent years! Some tips:\n1. Look for Buddhist temple restaurants (shojin ryori)\n2. Try Ain Soph chain for vegan options\n3. T's TanTan in Tokyo Station for vegan ramen\n4. Use the HappyCow app to find vegetarian restaurants\n5. Learn the phrase 'watashi wa bejitarian desu' (I am vegetarian)\n\nMany places now have English menus too."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    @"(GOSS NET 1) Tape 38/3 Page 180

AC 1 or AC 2. After 3 minutes, verify glycol discharge secondary pressure 39 to 51 psig. Also verify secondary EVAP out TEMP has changed. Next step, secondary coolant loop pump, OFF. Secondary glycol to radiator valve, BYPASS. That ends the procedure. Over.

02 10 45 52 CMP
Okay. I read back: monitor secondary accumulator quantity; secondary glycol radiator valve, NORMAL for 30 seconds, then to BYPASS. If no decrease in secondary accumulator quantity, secondary glycol to radiator valve to NORMAL. Secondary coolant loop pump, AC 1 or 2. After 3 minutes, verify glycol secondary discharge pressure 39 to 51 psig. Verify secondary evaporator outlet TEMP has changed. Secondary coolant loop pump, OFF. Secondary glycol radiator valve to BYPASS. And what's the reason for the change, Charlie?

02 10 46 27 CC
Roger. Stan is concerned that our present procedure as shown in the checklist does not really flow glycol through the radiator, and it - They want to verify that we do not have a plugged secondary radiator. Over.

02 10 46 48 CMP
Okay. They have any abnormal indications in that system, so far?

02 10 46 52 CC
Negative. This is the procedure that they came up with. It's just a check, Mike. Everything's looking great to us. Over.

02 10 47 02 CMP
Okay. Fine.

02 10 47 31 CDR
Charlie, we'll get back with you on these other changes in a few minutes. Okay?

02 10 47 38 CC
Roger, Neil. No hurry. Over.

02 11 10 57 CC
11, Houston. We're wondering who's on horn.

02 11 11 09 CMP
Say again, Houston?

02 11 11 12 CC
We just had a little music there.

02 11 11 21 CMP
We'll keep you entertained.

02 11 11 28 CC
Roger. That was good. You can keep it coming down, 11.

02 11 11 36 CMP
Okay.



(GOSS NET 1) Tape 38/4 Page 181

02 11 12 34 CMP
Because it's a special occasion today, Houston. This is the third anniversary of Gemini 10.

02 11 12 42 CC
Roger. Happy anniversary.

02 11 12 51 CMP
Thank you, sir.

02 11 19 32 LMP
Houston, Apollo 11. Ready to copy your update.

02 11 19 35 CC
Roger. Stand by.

02 11 19 50 CC
Okay, Buzz. The first item is that we have indications that your landing site obliques are not in the proper position. If you'll check, we think that the intermediate scale landing site oblique is stowed in the CSM lunar landmark book. We think that the large scale landing site oblique is stowed in the back of the LM lunar surface map book. Over.

02 11 20 38 LMP
I think I heard you Charlie, but I'm not sure that I understand.

02 11 20 45 CC
Roger. According to our stowage list the - the landing site oblique should be in the transfer bag. In - But in the backup set of data, the intermediate scale oblique is in the CSM lunar landmark book, and the large scale oblique is in the back of the LM lunar surface map book; and that's the reason we think that they might be not where you think they are. Over.

02 11 21 15 LMP
Okay. We've got three obliques. The last one is one I asked for recently. It's just a blow up of the second one. The first one is one that's got dotted lines on it, indicating horizon view and 50-degree LPD; and all three of those are in the transfer book. Over.

02 11 21 34 CC
Roger. Fine. We were wrong in our - in our backup set. We had those out of place. Looks like the onboard data's good. We just wanted to let you check on that one. We have an update on the APS/DPS fuel card that you place on the panel. It's a typo error. If you'll break out that little card, we've got the - correct that typo error. Over.

02 11 22 10 LMP
Stand by.

02 11 23 05 LMP
Okay. I got it.



(GOSS NET 1) Tape 38/5 Page 182

02 11 23 07 CC
Roger, Buzz. Under the DPS column on the pressure side, you go down to the fourth item, says pressure greater than 150 PTCA should be greater than 65 percent. Over.

02 11 23 28 LMP
Okay. That's greater than 1.8 but less than 65 and greater than 150 for greater than 65.

02 11 23 35 CC
That's affirmative. Out.

02 11 23 46 CC
And we have three items on the mission rules NO-GO card, if you're ready to copy those. Over.

02 11 24 32 LMP
Okay. I've got the mission rules NO-GO.

02 ll 24 34 CC
Roger, Buzz. First entry's on the EPS under AC bus A. The line extends all the way to high gate. Actually, the line should read - at DOI it would be NO-GO AC bus A. After that the NO-GO would be both buses. So if you will just pencil in both buses from PDI through high gate, it'll be correct for that line. Over.

02 11 25 20 LMP
Okay. I've got that: AC bus A for DOI and both buses NO-GO for PDI on.

02 11 25 28 CC
That's affirmative up until high gate. You can stop at the line in front of the column ""5 minutes to low gate"". Now the next line is under the G&C PGNS, pitch and roll GDA's. You can scratch that line completely. Over.

02 11 25 56 LMP
Roger. Got it.

02 11 25 57 CC
Okay, Buzz. Last entry is down under RCS, and it's a typo error under the three - in the line ""three axis attitude control"". If you proceed to the right at PDI plus 05, you'll see one axis. The line goes all the way to low gate to touchdown. That's incorrect. The line should stop under ""5 minutes to low gate"". Over.

02 11 26 38 LMP
Okay. We're stopping that at ""5 minutes to low gate"".

02 11 26 41 CC
That's affirmative. That completes that card. The rest of the updates are just really for your information based on our 58 hour platform - look at the platform. We are really good shape. Your GYRO's have almost no drift in them since - plotted update we were looking at X of a minus 2.24 ...



(GOSS NET 1) Tape 38/6 Page 183

Y of point plus 0.87, Z of minus 0.11. Since the update, which was based on the 52 hour P52, I believe, we gave you an X drift of plus 0.79, yaw of plus 1.06, Z of plus 0.02 ... I can see the difference between the 52 hour and the 57 hour alignments were - did not really give us enough time to get a real good or completely valid update on the drift check. So we're real satisfied with the way the GYRO's are looking. The PIPA's are looking great also. We are in real good shape with those, too. Over.

02 11 32 21 CDR
This is Apollo 11. Radio check.

02 11 32 23 CC
Roger. Reading you five by. How me? Over.

02 11 32 28 CDR
Okay. Loud and clear. You cut out when you were talking about the platform - something about 52 hours and after that, we never heard you again.

02 11 32 37 CC
Roger. Guess we were changing antennas. Stand by. That's affirmative, 11. We were swapping antennas on you down here. Basically the word is that we have a real good platform, very small drift on the GYRO's, and very small drift in the PIPA's. Over.

02 11 33 00 CDR
Roger. Thank you. And I would like to have a few words of clarification if you will give them to me on the RCS ... what that change of pitch may mean.

02 11 33 14 CC
Copy. A few words of clarification on the RCS? Oh. Roger. The update there, Neil, you are speaking of about the one axis down to 5 minutes of low gate?

02 11 33 31 CDR
Yes. That's right. I'm not quite sure what that ... means beyond 5 minutes.

02 11 33 38 CC
Stand by. I'll make sure I got my story straight with Control. Stand by.

02 11 33 44 CDR
Okay.

02 11 34 33 CC
11, Houston. On the RCS, what we are saying is that if we lose control about one axis prior to low gate, we would recommend an abort. This would require a - a loss of - of two distinct jets which is not very probable, but that is what we are recommending. After low gate we would continue on. We would recommend that we continue on to attempt a landing. Over.



(GOSS NET 1) Tape 38/7 Page 184

02 11 35 13 CDR
Roger. I think I ...

02 11 35 17 CC
Roger.

02 11 35 45 CMP
Charlie, did you say you had some updates for me from the lunar surface book?

02 11 35 49 CC
Apollo 11, say again. You were cut out. Over.

02 11 35 55 CMP
Roger. Did you say you had some updates for us in the lunar surface book. Over.

02 11 36 00 CC
Negative. At this time, we do not have any updates for the lunar surface book. We wanted you to have it just in case. Over.

02 11 36 12 CMP
Roger. You were cut out that time.

02 11 36 15 CC
Roger. At the present time, we do not have any updates for you on the lunar surface book. We are thinking about some and kicking them around, but they're very minor changes. Over.

02 11 36 37 CC
11, Houston. Did you copy that transmission?

02 11 37 09 CC
Apollo 11, Houston. We swapped antennas on you again. I say again that we do not have any lunar surface update - book updates at this time. We're considering a few minor ones, but we're still kicking them around the MOCR. Over.

02 11 37 27 CMP
Apollo 11, I understand.


END OF TAPE


APOLLO 11 AIR-TO-GROUND VOICE TRANSCRIPTION

(GOSS NET 1) Tape 39/1 Page 185

02 11 45 31 CDR
Houston, 11. We have a crew status report for you.

02 11 45 34 CC
Roger. Go ahead, 11.

02 11 45 39 CDR
Okay. Radiation: CDR 11009, CMP 10010, LMP 09011. No medication.

02 11 45 55 CC
Roger, 11. We copy for the radiations. And, we're considering - this PTC looks sort of weird to us, so we're considering stopping and starting over again, and we'll be with you in a couple of minutes. Over.

02 11 46 15 CDR
Okay.

02 11 47 46 CC
Apollo 11, Houston. Would you give us the LM/CM DELTA-P reading? Over.

02 11 48 45 CC
Hello, Apollo 11. Houston. We switched the antennas on you again. Would you please give us the LM/CM DELTA-P reading? Over.

02 11 49 43 CC
Hello, Apollo 11. Houston. Over.

02 11 49 48 CDR
Go ahead. 11 here.

02 11 49 50 CC
Roger. We switched antennas on you, there, moments ago, Neil. Could you please give us the LM/CM DELTA-P reading? Over.

02 11 50 00 CDR
It's less than 0.1.

02 11 50 02 CC
Roger.

02 11 50 13 CMP
0.15, now, Neil says, Charlie.

02 11 50 16 CC
Roger. Thank you, Mike. Could you give us some help? This PTC is strange: it's not like anything we've seen before. We were wondering if you have had any events or any odd data that could help us out. Over.

02 11 50 35 CMP
I didn't understand that. Say again.

02 11 50 38 CC
Roger. We're looking at a - sort of a funny looking PTC. We've already drifted out to 70 degrees in pitch and we're wondering if you all have had any vents or any such thing as that, that could have caused us to pick up these rates to drive us off. Over.



(GOSS NET 1) Tape 39/2 Page 186

02 11 51 09 CMP
Negative, Charlie. We don't know of anything.

02 11 51 11 CC
Roger.

02 11 51 13 CMP
Unless it's got something to do with that entry from the position that we want to be in. I don't know.

02 11 51 21 CC
Roger. When we started off, it looked real fine to us. Now it's drifting off with a funny pattern that we haven't seen previously on a flight. And we're just trying to figure out - I think we'll probably start it over again. We'll be with you momentarily. Over.

02 11 51 39 CMP
Okay.

02 11 55 12 CC
Apollo 11, Houston. We hate to say it, but we'd like to terminate this PTC and start over again. We have no assurance that we're going to get it through the sleep period with this funny configuration, or funny pattern. We'd like you to stop it now and go back to pitch 090, yaw 0, and roll - whatever you stop on. Over.

02 11 55 42 CMP
Roger.

02 11 58 37 CC
11, Houston. We recommend you disable Bravo and Charlie; select quads Alfa and Delta. Over.

02 11 59 25 CC
Apollo 11, Houston. Over.

02 12 02 30 CC
Apollo 11, Houston. Over.

02 12 03 07 CC
Apollo 11, Houston. Over.

02 12 03 40 CC
Apollo 11, Houston. Over.

02 12 05 55 CC
Apollo 11, Houston. Do you read? Over.

02 12 11 09 CC
Hello, Apollo 11. Hello, Apollo 11. Over.

02 12 11 32 CDR
... Apollo 11. Go ahead.

02 12 11 35 CC
Roger. Reading you about one-by. Looks like we picked a super attitude here for PTC stabilization. We're reading you in BACKUP VOICE now. Over.

02 12 11 51 CDR
Roger. We read you loud and clear.

02 12 11 53 CC
Roger.



(GOSS NET 1) Tape 39/3 Page 187

02 12 12 03 CDR
Would you like us to pick another antenna?

02 12 12 06 CC
I think we've got about the best configuration. We've been doing it all from the ground here, 11. We'll just keep it as it is. Over.

02 12 12 16 CDR
Roger.

02 12 14 32 CC
Apollo 11, Houston. Would you select COMMAND RESET and OMNI Alfa? Over.

02 12 15 03 CDR
Houston, 11 on OMNI Alfa.

02 12 15 06 CC
Roger. We read you about three-by now. Over.

02 12 15 12 CDR
Roger.

02 12 17 19 CC
Apollo 11, Houston. We is stable; you can start the PTC. Over.

02 12 17 29 SC
...

02 12 20 59 CMP
Houston, Apollo 11. Checklist page F9 7; I've completed step 8, and I'd like to know what you think is ideal timing between step 8, step 9, and step 10 on that page? Over.

02 12 21 15 CC
Roger. Stand by.

02 12 22 46 CC
Apollo 11, Houston. We don't see any time constraint. We'd like you to go ahead and set up the wide deadband and then go through step 10 and 11. Over.

02 12 22 57 CMP
Okay. Will do. I don't see any constraint here, Charlie. I was just checking to make sure, because last time, I went from 8 to 9 to 10 to 11 a little bit more swiftly than I'd been doing in the past.

02 12 23 09 CC
Roger.

02 12 23 58 CMP
Step 11 complete.

02 12 24 02 CC
Roger. We copy.

02 12 24 22 CC
Apollo 11, Houston. Would you please select OMNI Bravo? Over.

02 12 24 30 CMP
Roger. Bravo."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "If you're staying only in Tokyo, a JR Pass isn't necessary - it's better for traveling between cities. Instead, get a Suica or Pasmo IC card. You can use it on all trains, subways, and buses in Tokyo. Just tap to enter and exit. Very convenient!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Makes sense. How much should I budget for daily transportation?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "For daily transportation within Tokyo, budget about ¥1,000-1,500 per day ($7-10 USD). If you're making many trips, consider a 24-hour or 72-hour Tokyo Metro pass, which can be more economical at ¥600-1,500 depending on the duration."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "This is all so useful! One more thing - are there any cultural customs I should be aware of?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Yes! Here are some important ones:\n1. Remove shoes when entering homes, some restaurants, and temples\n2. Don't eat while walking\n3. Bow when greeting people\n4. Be quiet on trains - no phone calls\n5. Don't tip - it can be seen as insulting\n6. Use both hands when giving or receiving business cards\n7. Don't stick chopsticks upright in rice\n8. Stand on the left on escalators (right in Osaka)\n\nJapanese people are very forgiving with tourists though!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I didn't know about the chopstick thing! Why is that considered rude?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Great question! Sticking chopsticks upright in rice resembles incense sticks at a funeral and is associated with death in Japanese culture. It's only done at funerals when offering rice to the deceased. Similarly, passing food from chopstick to chopstick is also taboo as it mimics a funeral ritual of passing cremated bones."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Wow, I definitely want to avoid that! Should I learn some basic Japanese phrases?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Yes! Even a few phrases go a long way:\n- Konnichiwa (Hello)\n- Arigatou gozaimasu (Thank you)\n- Sumimasen (Excuse me/Sorry)\n- Hai (Yes) / Iie (No)\n- Eigo ga hanasemasu ka? (Do you speak English?)\n- Oishii! (Delicious!)\n- O-kanjou kudasai (Check please)\n\nMost people appreciate the effort even if your pronunciation isn't perfect!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I'll practice those! What about internet access? Should I get a pocket WiFi?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Pocket WiFi is a good option! You can rent one at the airport for about ¥800-1,000 per day. Alternatively, you could get a prepaid SIM card if your phone is unlocked. Many convenience stores and airports sell them. There's also free WiFi in many stations, convenience stores, and cafes now."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I think I'll go with pocket WiFi. Can I pick it up at Narita Airport?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Yes! There are several rental counters at Narita Airport. Companies like Japan Wireless, Global Advanced Communications, and Ninja WiFi have booths in both terminals. I'd recommend reserving online before your trip - you'll get better rates and guaranteed availability."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Perfect! How long does it take to get from Narita to Shinjuku?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "You have a few options:\n1. Narita Express (N'EX) - About 80 minutes direct to Shinjuku, ¥3,250\n2. Keisei Skyliner to Ueno, then transfer - About 90 minutes total, ¥2,600\n3. Airport Limousine Bus - About 90-120 minutes depending on traffic, ¥3,200\n\nThe N'EX is the most convenient for Shinjuku as it's a direct connection."
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "I think that covers everything! You've been incredibly helpful. Thanks so much!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "You're very welcome! Have an amazing trip to Tokyo! Don't hesitate to ask if you have any other questions while planning. Safe travels!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Actually, one more quick question - what about cash vs credit cards?"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "Good question! Japan is still largely a cash-based society, though this is changing. Tips:\n1. Bring enough cash - many small restaurants and shops are cash-only\n2. 7-Eleven and Japan Post Bank ATMs accept international cards\n3. Credit cards work at major hotels, department stores, and chain restaurants\n4. Carry around ¥10,000-20,000 in cash daily\n5. IC cards like Suica can be used at many convenience stores and vending machines\n\nAlways have cash as a backup!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Great advice! Now I'm really ready. Thanks again!"
));

contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.Assistant,
    "My pleasure! Enjoy Tokyo - it's an incredible city. さようなら (Sayounara - Goodbye)!"
));
#endregion

ToolContextService toolContextService = new ToolContextService(api, contextContainer);

# region Tool Definitions


#endregion

TaskContextService taskContextService = new TaskContextService(api, contextContainer);
ModelContextService modelContextService = new ModelContextService(api, contextContainer);

modelContextService.AddModelToLibrary("expensive", ChatModel.OpenAi.Gpt35.Turbo, "Best for general purpose tasks with high accuracy.");
modelContextService.AddModelToLibrary("cheap", ChatModel.OpenAi.Gpt35.Turbo, "Good for less complex tasks where cost is a concern.");
modelContextService.AddModelToLibrary("ethical", ChatModel.OpenAi.Gpt35.Turbo, "Useful for tasks requiring strong safety and ethical considerations.");
modelContextService.AddModelToLibrary("thinking", ChatModel.OpenAi.Gpt35.Turbo, "Well-rounded and powerful model across domains. It sets a new standard for math, science, coding, and visual reasoning tasks. It also excels at technical writing and instruction-following. Use it to think through multi-step problems that involve analysis across text, code, and images");

InstructionContextService instructionsContextService = new InstructionContextService(api, contextContainer);
MessageContextService messageContextService = new MessageContextService(api, contextContainer);

ContextController contextManager = new ContextController(
    taskContextService,
    contextContainer,
    instructionsContextService,
    toolContextService,
    modelContextService,
    messageContextService
);

//AgentContext context = await contextManager.GetAgentContext();


ContextAgent agent = new ContextAgent(api, contextManager);

var response = await agent.RunAsync(new ChatMessage(ChatMessageRoles.User,"Can you help me plan a trip to Tokyo for a week? I want to see the main sights, try local food, and experience the culture. Also, any tips for getting around and staying connected would be great."));

