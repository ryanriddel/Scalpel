package natsmqAdapter;

import io.nats.client.*;


import io.nats.client.Message;
import org.apache.activemq.ActiveMQConnection;
import org.apache.activemq.ActiveMQConnectionFactory;
import org.apache.camel.*;

import org.apache.camel.builder.RouteBuilder;
import org.apache.camel.component.jms.JmsComponent;
import org.apache.camel.component.jms.JmsMessage;
import org.apache.camel.component.jms.MessageCreatedStrategy;
import org.apache.camel.component.nats.NatsComponent;
import org.apache.camel.component.nats.NatsConfiguration;
import org.apache.camel.component.nats.NatsConsumer;
import org.apache.camel.component.nats.NatsEndpoint;
import org.apache.camel.impl.DefaultCamelContext;
import org.apache.camel.main.Main;

import org.apache.camel.main.MainListenerSupport;
import org.apache.camel.main.MainSupport;
import org.apache.camel.spi.DataType;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jms.support.converter.MessageConversionException;
import org.springframework.jms.support.converter.MessageConverter;
import org.springframework.util.ErrorHandler;
import sun.rmi.runtime.Log;

import javax.jms.ConnectionFactory;
import javax.jms.JMSException;
import javax.jms.Session;
import javax.jms.TextMessage;
import javax.servlet.*;
import javax.xml.datatype.DatatypeConstants;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.util.Enumeration;
import java.util.Locale;
import java.util.Map;

import org.apache.camel.Processor;

/**
 * A Camel Application
 */

public class MainApp {
    @Produce(uri = "activemq:queue:nifiQueue3")
    private ProducerTemplate activeMQProducerTemplate;
    /**
     * A main() so we can easily run these routing rules in our IDE
     */
    private Main main;
    private CamelContext camContext;

    public static void main(String[] args) throws Exception {
        MainApp example = new MainApp();
        example.boot();
    }

    public void boot() throws Exception {
        // create a Main instance
        main = new Main();
        System.out.println("Running that nats-mq adapter....");

        CamelContext camContext = new DefaultCamelContext();


        ConnectionFactory connFactory = new ActiveMQConnectionFactory("tcp://localhost:61616");
        JmsComponent jmsComp = JmsComponent.jmsComponentAutoAcknowledge(connFactory);
        jmsComp.setAcceptMessagesWhileStopping(false);
        jmsComp.setAllowNullBody(true);

        jmsComp.setAsyncConsumer(true);
        jmsComp.setDeliveryPersistent(false);
        jmsComp.setErrorHandler(new ErrorHandler() {
            @Override
            public void handleError(Throwable throwable) {
                System.out.println("Error!!!");
            }
        });
        jmsComp.setMessageTimestampEnabled(true);
        camContext.addComponent("jms", jmsComp);


    /*
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
            AsyncSubscription sub = natsConnection.subscribe("MKTDATA.>", new MessageHandler() {
                @Override
                public void onMessage(io.nats.client.Message message) {
                    String messageText = new String(message.getData());
                    String subjectText = new String(message.getSubject());

                    System.out.println("Received: " + messageText);
                    activeMQProducerTemplate.sendBody(messageText);
                }
            });
            NatsComponent nComp = new NatsComponent();
            nComp.setCamelContext(camContext);

            NatsConfiguration nConf = new NatsConfiguration();

            Endpoint natsEndpoint = nComp.createEndpoint("nats://localhost:4222");

            camContext.addEndpoint("nats-comp", natsEndpoint);
            camContext.addRoutes(new RouteBuilder() {
                @Override
                public void configure() throws Exception {
                    from("nats-comp")
                    .to("jms:queue:nifiQueue2");
                }
            });

        }

*/



        /*
        context.addRoutes(new RouteBuilder() {
            public void configure() {
                from("nats://localhost:4222?topic=MKTDATA.>")
                        .removeHeaders("*")
                        .to("test-jms-nifi:queue:test.queue");
            }
        });
*/

        /*context.addRoutes(new RouteBuilder() {
            @Override
            public void configure() throws Exception {
                from("nats://localhost:4222?topic=MKTDATA.*.*.*.*.*.*.*.*.*")
                        .removeHeader("*")
                        .to("test-jms-nifi:queue:test.queue2");
            }
        });*/

        ProducerTemplate template = camContext.createProducerTemplate();

        // add routes
        main.addRouteBuilder(new MyRouteBuilder());
        // add event listener
        main.addMainListener(new Events());
        // set the properties from a file
        main.setPropertyPlaceholderLocations("example.properties");
        // run until you terminate the JVM

        System.out.println("Starting Camel. Use ctrl + c to terminate the JVM.\n");
        main.run();
    }




    public class Events extends MainListenerSupport {

        @Override
        public void afterStart(MainSupport main) {
            System.out.println("MainExample with Camel is now started!");
        }


        @Override
        public void beforeStop(MainSupport main) {
            System.out.println("MainExample with Camel is now being stopped!");
            try {
                camContext.stop();
            } catch (Exception e) {
                e.printStackTrace();
            }

        }
    }

    public static void consoleWrite(String arg)
    {
        System.console().writer().println(arg);
    }

}

