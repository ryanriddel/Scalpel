package scalpel.magicbus;

// Imports the Google Cloud client library
import com.google.auth.Credentials;
import com.google.auth.oauth2.ServiceAccountCredentials;
import com.google.cloud.bigquery.*;
import com.google.protobuf.InvalidProtocolBufferException;
import io.nats.client.*;
import org.apache.camel.main.Main;
import org.joda.time.DateTime;
import org.joda.time.LocalDate;

import java.io.FileInputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;


/**
 * A Camel Application
 */


public class MainApp {
    private static final String TABLE_ID = "OPRA";
    public static final String PROJECT_ID = "custom-frame-179618";
    public static final String DATASET_ID = "TRADES";


    // Static variables for API scope, callback URI, and HTTP/JSON functions
    private static final String REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";


    public static final String GCP_CREDENTIALS_FILE_LOCATION = "/home/r_riddel/gcloud-credentials.json";
    public static final String WINDOWS_CREDENTIALS_FILE_LOCATION = "C:\\credentials.json";
    public static final String NY4_CREDENTIALS_FILE_LOCATION = "/home/scalp/scalpel/gcloud-credentials.json";

    public static final String TRADE_SCHEMA_JSON_FILE_LOCATION = "trade-schema.json";

    public static BigQuery bq;

    //static TableSchema tradeTableSchema;
    /**
     * A main() so we can easily run these routing rules in our IDE
     */
    public static void main(String... args) throws Exception {
        Main main = new Main();


        FileInputStream credentialsInput = new FileInputStream(NY4_CREDENTIALS_FILE_LOCATION);


        BigQueryOptions.Builder bqBuilder = BigQueryOptions.newBuilder();
        Credentials gcpCredentials = ServiceAccountCredentials.fromStream(credentialsInput);

        bqBuilder.setCredentials(gcpCredentials);
        bqBuilder.setProjectId(PROJECT_ID);
        BigQueryOptions bqOpts = bqBuilder.build();

        BigQuery bq = bqOpts.getService();



        Dataset tradeDataset = bq.getDataset("TRADES");
        Table tradeTable = tradeDataset.get("OPRA");

        //Test to make sure bq works:
        System.out.println("BQ test: " + tradeDataset.getDescription());



        TableId tableId = TableId.of("TRADES", "OPRA");


        //InsertAllRequest irRes = bq.insertAll(InsertAllRequest.newBuilder("TRADES","OPRA").)


        //TableId testTableID = TableId.of("TRADES", "TEST");
        // Table field definition
        ArrayList<com.google.cloud.bigquery.Field> fieldList = new ArrayList<>();
        fieldList.add(com.google.cloud.bigquery.Field.of("TIMESTAMP", LegacySQLTypeName.STRING));
        fieldList.add(com.google.cloud.bigquery.Field.of("PRICE", LegacySQLTypeName.STRING));
        fieldList.add(com.google.cloud.bigquery.Field.of("SIZE", LegacySQLTypeName.STRING));
        fieldList.add(com.google.cloud.bigquery.Field.of("SYMBOL", LegacySQLTypeName.STRING));

        Schema schema = Schema.of(fieldList);
        StandardTableDefinition tableDefinition = StandardTableDefinition.of(schema);


                //*********************NATS

        io.nats.client.Connection natsConnection;
        io.nats.client.Options natsOptions;
        io.nats.client.Options.Builder b = new io.nats.client.Options.Builder();
        b.errorCb(new ExceptionHandler() {
            @Override
            public void onException(NATSException e) {
                System.out.println("Error!  " + e.getLocalizedMessage());
            }
        });
        natsOptions = b.build();
        natsConnection = Nats.connect("nats://localhost:4222", natsOptions);



        if(!natsConnection.isConnected())
        {
            System.out.println("Nats object connection failed!!!");

        }
        else
        {
            System.out.println("Nats object connection succeeded!!!");
            AsyncSubscription sub = natsConnection.subscribe("MKTDATA.TRADE.>", new MessageHandler() {


                InsertAllRequest.Builder insertBuilder = InsertAllRequest.newBuilder("TRADES","OPRA");

                //Queue<HashMap<String, Object>> messageList = new ConcurrentLinkedQueue<HashMap<String, Object>>();
                final int bufferSize = 50;
                final int updateRate = 2000;
                int count = 0;

                        @Override
                        public void onMessage(io.nats.client.Message message) {
                            //String messageText = new String(message.getData());
                            //String subjectText = new String(message.getSubject());

                            Map<String, Object> msg = ParseProtobufFromNATS(message);

                            if(msg != null) {
                                insertBuilder.addRow(msg);
                                count++;
                                //messageList.add(msg);

                                if (count % bufferSize == 0) {

                                    try {

                                        //new Thread(() -> InsertRows(insertBuilder.build())).start();
                                        InsertAllRequest req = insertBuilder.build();

                                        InsertRows(req, bq);

                                        insertBuilder = InsertAllRequest.newBuilder("TRADES", "OPRA");

                                    } catch (Exception e) {
                                        System.out.println("Threading error? " + e.getMessage());
                                    }
                                }
                            }

                            if(count % updateRate == 0)
                            {
                                System.out.println("Messages processed: " + count);
                                System.out.println("NATS STATS: Dropped = " + message.getSubscription().getDropped() +
                                                           ", Delivered = " + message.getSubscription().getDelivered() + ", Pending = " +
                                                           message.getSubscription().getPendingMsgs());

                            }


                            //System.out.println("Received: " + messageText);
                        }
                    });
            System.out.println("Subscribed to MKTDATA.TRADE.>");
        }


        main.run(args);


    }
    
    public static void InsertRows(InsertAllRequest req, BigQuery bquery)
    {
        InsertAllResponse response;
        try {
            response = bquery.insertAll(req);
        }
        catch (Exception e)
        {
            if(e!=null)
                System.out.println("Insert Rows error: " + e.getMessage());
            else
                System.out.println("e is actually null....?");
            return;
        }

        if(response.hasErrors())
        {
            for (Map.Entry<Long, List<BigQueryError>> entry : response.getInsertErrors().entrySet()) {
                // inspect row error
                for (BigQueryError error : entry.getValue()) {
                    System.out.println("InsertAll error: " + error.getMessage());

                }
            }
        }
        else
        {


        }

    }
    static int counter2 = 0;
    public static HashMap<String, Object> ParseProtobufFromNATS(io.nats.client.Message message)
    {
        HashMap<String, Object> rowContent = new HashMap<>();

        byte[] msgAr = message.getData();
        String subjectText = message.getSubject();
        String[] subjectParts = subjectText.split("\\.");


        if(subjectParts[1].contains("TRADE"))
        {
            Mktdata.TradeMessage tMsg = null;
            Mktdata.Instrument tInstr = null;
            try {
                tMsg = Mktdata.TradeMessage.parser().parseFrom(msgAr);
                tInstr = tMsg.getInstruments(0);
            } catch (InvalidProtocolBufferException e) {
                System.out.println("ParseProtobuf error: ");
                e.printStackTrace();
                return null;
            }

            try {
                String expiration = (new LocalDate(2000 + tInstr.getExpirationYear(), tInstr.getExpirationMonth(),
                                                          tInstr.getExpirationDay())).toString();

                rowContent = CreateBigQueryTradeMessage(tMsg.getTimestamp(), tMsg.getExchange(), tMsg.getPrice(), tMsg.getSize(),
                        tMsg.getDaysTotalVolume(), tInstr.getUnderlyingSymbol(), expiration,
                        tInstr.getIsCallOption(), tInstr.getStrike());

                if (rowContent.containsKey("TIMESTAMP"))
                    return rowContent;
                else
                    return null;
            }
            catch(Exception e)
            {
                System.out.println("Error in ParseProtobuf: " + subjectText);
                return null;
            }
        }
        else if(subjectParts[1] == "DEPTH")
        {
            System.out.println("DEPTH MSG RECEIVED?");
        }
        else
        {
            System.out.println("Unrecognized header: "  + subjectParts[1]);
        }

        return null;
    }


    public static HashMap<String, Object> CreateBigQueryTradeMessage(long timestamp, String exchange, float price, int size, int totvol,
                                                  String underlying, String expiration, boolean iscalloption, float strike)
    {
        HashMap<String, Object> rowContent = new HashMap<>();

        DateTime now = DateTime.now();
        Integer ts = (int) timestamp;

        Integer hour = (int) (ts/10000000);
        Integer minute = (int) (ts/100000 - hour*100);
        Integer second = (int) (ts/1000 - hour*10000 - minute*100);
        Integer millisecond = ts - hour*10000000 - minute*100000 - second*1000;
        DateTime d = new DateTime(now.getYear(), now.getMonthOfYear(), now.getDayOfMonth(), hour, minute, second, millisecond);
        rowContent.put("TIMESTAMP", (double) (d.getMillis()/1000));

        rowContent.put("EXCHANGE", exchange);
        rowContent.put("PRICE", price);
        rowContent.put("SIZE", size);
        rowContent.put("TOTVOL", totvol);
        rowContent.put("UNDERLYING", underlying); //e.x. TWTR
        rowContent.put("EXPIRATION",expiration);
        rowContent.put("ISCALLOPTION", iscalloption);
        rowContent.put("STRIKE", strike);

        return rowContent;
    }


    public static HashMap<String, Object> CreateBigQueryMessage(io.nats.client.Message message)
    {
        HashMap<String, Object> rowContent = new HashMap<>();
        String messageText = new String(message.getData());
        String subjectText = new String(message.getSubject());

        String[] parts = messageText.split(",");
        String[] subjectSplit = subjectText.split("\\.");

        try {

            //convert timestamp:
            DateTime now = DateTime.now();
            Integer ts = Integer.parseInt(parts[0]);
            Integer hour = (int) (ts/10000000);
            Integer minute = (int) (ts/100000 - hour*100);
            Integer second = (int) (ts/1000 - hour*10000 - minute*100);
            Integer millisecond = ts - hour*10000000 - minute*100000 - second*1000;


            DateTime d = new DateTime(now.getYear(), now.getMonthOfYear(), now.getDayOfMonth(), hour, minute, second, millisecond);

            rowContent.put("TIMESTAMP", (double) (d.getMillis()/1000));

            rowContent.put("EXCHANGE", parts[1]);
            rowContent.put("PRICE", Float.parseFloat(parts[2]));
            rowContent.put("SIZE", Integer.parseInt(parts[3]));
            rowContent.put("TOTVOL", Integer.parseInt(parts[4]));
        }
        catch (Exception e)
        {
            System.out.println("Error in first part of message creation: " + e.toString() + "\n" +  e.getMessage());
        }

        try
        {
            if(subjectSplit.length >=9)
            {
                //rowContent.put("DATUMDOMAIN", subjectSplit[0]); //e.x. MKTDATA
                //rowContent.put("TICKTYPE", subjectSplit[1]); //e.x. TRADE
                //rowContent.put("INSTRTYPE", subjectSplit[2]); //e.x. OPT
                rowContent.put("UNDERLYING", subjectSplit[3]); //e.x. TWTR

                rowContent.put("EXPIRATION",(new LocalDate(2000 + Integer.parseInt(subjectSplit[4]), Integer.parseInt(subjectSplit[5]), Integer.parseInt(subjectSplit[6]))).toString());

                rowContent.put("ISCALLOPTION",  (subjectSplit[7] == "1" ? true : false));

                if (subjectSplit.length == 9)
                    rowContent.put("STRIKE", Float.parseFloat(subjectSplit[8] +".00"));
                else if(subjectSplit.length == 10)
                    rowContent.put("STRIKE", Float.parseFloat(subjectSplit[8] +"." + subjectSplit[9]));
                else
                    System.out.println("Strange error: " + subjectText);

            }
        }
        catch (Exception e)
        {
            System.out.println("Error in second part of message creation: " + e.toString() + "\n" + e.getMessage());
        }

        return rowContent;
    }





}

