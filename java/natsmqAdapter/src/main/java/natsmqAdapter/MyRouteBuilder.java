package natsmqAdapter;

import com.sun.javafx.collections.MappingChange;
import io.nats.client.Nats;
import org.apache.activemq.command.ActiveMQMessage;
import org.apache.camel.*;
import org.apache.camel.builder.RouteBuilder;

import javax.jms.ConnectionFactory;
import javax.jms.JMSException;
import javax.jms.TextMessage;

import org.apache.activemq.ActiveMQConnectionFactory;
import org.apache.camel.builder.RouteBuilder;
import org.apache.camel.component.jms.JmsComponent;
import org.apache.camel.component.jms.JmsMessage;
import org.apache.camel.component.jms.JmsMessageType;
import org.apache.camel.component.nats.NatsConfiguration;
import org.apache.camel.component.nats.NatsConstants;
import org.apache.camel.impl.DefaultCamelContext;
import org.apache.camel.Exchange;
import org.apache.camel.Processor;
import org.apache.camel.spi.DataType;

import java.util.Collection;
import java.util.Map;
import java.util.Set;

/**
 * A Camel Java DSL Router
 */
public class MyRouteBuilder extends RouteBuilder {

    /**
     * Let's configure the Camel routing rules using Java code...
     */
    @Override
    //forward NATS to ActiveMQ in a Nifi-friendly format
    public void configure() throws Exception {
        from("nats://localhost:4222?topic=MKTDATA.>")
                .outputType(TextMessage.class)
                .process(new PrepareForNiFi())
                .convertBodyTo(String.class)
                .threads(2)
                .to("activemq://topic:nifiQueue?deliveryPersistent=false&mapJmsMessage=false");

        //forward NATS to the cloud
        from("nats://localhost:4222?topic=MKTDATA.TRADE.>")
                .outputType(io.nats.client.Message.class)
                .process(new AddHeadersToMessage())
                .toD("nats://35.193.48.248:4222?topic=${header.TOPIC}");


    }

}

class PrepareForNiFi implements Processor {

    public void process(Exchange exchange) throws Exception {
        io.nats.client.Message body = exchange.getIn().getBody(io.nats.client.Message.class);

        String bodyText = new String(body.getData());
        String[] parts = bodyText.split(",");

        String subject = body.getSubject();

        String[] subjectSplit = subject.split("\\.");

        Message m = exchange.getIn();

        // m.removeHeaders("*");
        if (parts.length == 5) {
            m.setHeader("Timestamp", parts[0]);
            m.setHeader("Exchange", parts[1]);
            m.setHeader("Price", parts[2]);
            m.setHeader("Size", parts[3]);
            m.setHeader("TotVol", parts[4]);
        } else
            return;

        m.setHeader("TOPIC", subject);
        m.setHeader("BODY", bodyText);

        if (subjectSplit.length == 9 || subjectSplit.length == 10) {
            m.setHeader("DATUMDOMAIN", subjectSplit[0]); //e.x. MKTDATA
            m.setHeader("TICKTYPE", subjectSplit[1]); //e.x. TRADE
            m.setHeader("INSTRTYPE", subjectSplit[2]); //e.x. OPT
            m.setHeader("SYMBOL", subjectSplit[3]); //e.x. TWTR
            m.setHeader("EXPYEAR", subjectSplit[4]); //e.x. 17
            m.setHeader("EXPMONTH", subjectSplit[5]);
            m.setHeader("EXPDAY", subjectSplit[6]);
            m.setHeader("ISPUT", subjectSplit[7]);
            m.setHeader("STRIKEDOLLARS", subjectSplit[8]);
            if (subjectSplit.length == 9)
                m.setHeader("STRIKECENTS", 0);
            else
                m.setHeader("STRIKECENTS", subjectSplit[9]);
        }

        exchange.setIn(m);

    }
}
class AddHeadersToMessage implements Processor
{
    public void process(Exchange exchange) throws Exception {
        io.nats.client.Message body = exchange.getIn().getBody(io.nats.client.Message.class);

        String bodyText = new String(body.getData());
        String[] parts = bodyText.split(",");

        String subject = body.getSubject();

        Message m = exchange.getIn();
        m.setHeader("TOPIC", subject);
        m.setHeader("BODY", bodyText);

        exchange.setIn(m);

    }
}

