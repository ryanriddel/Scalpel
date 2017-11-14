# Scalpel
Contains code related to the Scalpel project

mdt: "Market data tool" capable of connecting to a NATS gateway and receiving/analyzing data from any number of different subscriptions. In the left panel, the IP and PORT boxes are already populated.  Set the "Protocol" box to NATS.  Next go to the subscriptions textbox, which is prepopulated with "MKTDATA.TRADE.OPT.>".  Click "Add to Subscriptions List", and you will see it added to the list below.  You can add more subscriptions, including ones for quote messages ("MKTDATA.DEPTH.>").  When you're finished, set the test duration updown controls and click Begin Test!.  When the test is finished, relevant data will be shown in the "Data" tab.
