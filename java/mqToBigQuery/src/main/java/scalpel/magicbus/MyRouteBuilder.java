package scalpel.magicbus;

import org.apache.camel.Exchange;
import org.apache.camel.Message;
import org.apache.camel.Processor;
import org.apache.camel.builder.RouteBuilder;

/**
 * A Camel Java DSL Router
 */
public class MyRouteBuilder extends RouteBuilder {

    /**
     * Let's configure the Camel routing rules using Java code...
     */
    public void configure() {

        // here is a sample which processes the input files
        // (leaving them in place - see the 'noop' flag)
        // then performs content based routing on the message using XPath

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
            m.setHeader("Timestamp", parts[0]);
            m.setHeader("Exchange", parts[1]);
            m.setHeader("Price", parts[2]);
            m.setHeader("Size", parts[3]);
            m.setHeader("TotVol", parts[4]);


            if(subjectSplit.length >=9)
            {
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

}
