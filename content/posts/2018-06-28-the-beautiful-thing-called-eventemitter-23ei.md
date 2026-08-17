---
title: The beautiful thing called EventEmitter
subtitle: ~
categories: node,eventemitter,javascript
abstract: Would you mind taking a look at EventEmitters?
date: 2018-06-28
language: en
---

Event emitters are a good way to do async communication between moving parts in code.
Event emitters are in a *diluted* simplification a *dictionary of functions with some helpers (generally: on, off, emit)*

so a very simple and naive implementation could be something like this

```js
// we'll omit error handling and complex stuff for simplicity
const EventEmitter = {
  events: {}, // dictionary with our events
  on(event, listener) { // add event listeners
    if (!this.events[event]) { this.events[event] = { listeners: [] } }
    this.events[event].listeners.push(listener);
  },
  off(event) { // remove listeners
    delete this.events[event]
  },
  emit(name, ...payload) { // trigger events
    for (const listener of this.events[name].listeners) {
      listener.apply(this, payload)
    }
  }
};

EventEmitter.on('dog', () => console.log('dog'));
EventEmitter.on('dog', (name, color, race) => console.log('dog', name, color, race));

EventEmitter.emit('dog');
// dog
// dog undefined undefined undefined

EventEmitter.emit('dog', 'Fig', 'brown', 'chihuahua');
// dog
// dog Fig brown chihuahua

EventEmitter.off('dog')

// EventEmitter.emit('dog');
// TypeError: Cannot read property 'listeners' of undefined

```



now if you had use this emitter thing before, perhaps you are thinking something like *Really? it is that simple?* Well generally speaking yes, but perhaps you want to adjust things for performance and scaling, error management, Etc,.

however, if you don't want to reinvent the wheel, you can just use node's implementation of an event emitter, I'm pretty sure it is already great since node's streams implement that interface.


the code is very similar when implemented:
```js
const EventEmitter = require('events');

const ee = new EventEmitter();

ee.on('dog', () => console.log('dog'));
ee.on('dog', (name, color, race) => console.log('dog', name, color, race));

ee.emit('dog');
// dog
// dog undefined undefined undefined

ee.emit('dog', 'Fig', 'brown', 'chihuahua');
// dog
// dog Fig brown chihuahua

```


so at this point you may be wondering why should you use this? after all we have tools for async code, like promises or callbacks, and you would assume well it's a fair argument.

In my opinion the common cases are when you need to react to certain events happening in the environment, take for example, browsers's click, you react to a click you don't ever know when it is going to happen, and promises or callbacks are more likely to be called in a more programmatic way, for example after you did something, keep doing this async task and call me when it's done to keep doing what I was going to do.

in other words take this other promise like example

>Hey Mark, Judith called, she wants to meet you at the cafeteria later, I told her you would call back to confirm (setting the promise)
// spends few minutes doing other things
Hey Judith, Mark here I'm on my way (fulfill promise)


Now let's try to make an example of a emitter

>Hey Steve, you need to guard this entrance, each time a person comes by, press this button to increment the counter Ok? (register event [on])
because I have reasons and things to do else where.
Sure Mark!
(emit) person comes by 3 mins later
**clicks**
(emit) person comes by 1 hour later
**clicks**


I hope it makes it a little bit clear
(emit) person comes by later
**clicks**

yeah, that might happen too :P

# Extending an event emitter
The event emitter is easy to extend in node:

```js
class MyEmitter extends EventEmitter {

}
```

and Boom, you can already use MyEmitter with *on*, *emit*, and the other cool features you can find in the [node docs](https://nodejs.org/docs/latest-v8.x/api/events.html)

let's do another example
```js

class MyEmitter extends EventEmitter {

  constructor(avilableTickets = 31) {
    super()
    this.ticketCount = avilableTickets
  }

  *dispenseTicket() {
    while (this.ticketCount > 0) {
      // check each 10 tickets
      if (this.ticketCount % 10 === 0) {
        // call something somewhere to act at the count number
        this.emit('spent-10-tickets', this.ticketCount)
      } else if (this.ticketCount < 10) {
        this.emit('warn:low-tickets', this.ticketCount)
      }
      yield --this.ticketCount;
    }
    this.emit('spent-all-tickets')
  }
}

const myee = new MyEmitter();

myee
  .on('spent-10-tickets', count => console.log(count))
  .on('warn:low-tickets', count => console.warn(`Warning! ticket count is low:${count}`));

const ticketDispenser = myee.dispenseTicket();
const interval = setInterval(() => ticketDispenser.next(), 500);

myee
  .on('spent-all-tickets', () => {
    console.log('no more tickets')
    clearInterval(interval)
  });
```
now we can use that ticket dispenser on other places in the code (simulated in this case by the set interval) where we're not calling this emitter directly

We're far more interested in knowing what's the state of our ticket count and react accordingly.

In the case of node you may find emitters in Stream Objects for example, so if you create a Write/Read Stream, you often use listeners on read/write events and on finish.

now I've used emitters more to register system events like un-handled promise errors or to track a stream's process and print that to the console for custom CLI-Tools, you usage may vary, a good use case could be WebSocket communication, since WebSockets aim to be a real time communication solution, it is very likely that these interactions happen at random times.


a complex use case I had once forced me to mix generators, and node streams.
Basically I needed to transform data from mongo collection with over hundreds of thousands of records to insert them into a new collection, this had to run on a weekly basis and well it had to do it with every record in the collection.

the solution had to be in node and my shot was to use a generator that pulled batches of n amount of records (it depended on the processing power of the server, 100, 600 you name it) records (like the ticket dispenser) then those records went into a transform node stream (in object mode) that did all the transforming stuff, and once it was done with that batch of records it pulled another and so on using just ***on*** and ***emit*** at the right places, so the processing always happened in controlled batches and the streams never clogged.

I Realize that it had a mixed solution, but I could have never done it (with my experience) without event emitters.


But be aware that using too much listeners can lead to performance problems, emitters are really powerful but if you use them too much, well you will have quite a bunch of performance problems.


Also take in mind that, any place where is code can call these emitters, so be careful not to run on spaghetti code as well as hidden listeners somewhere in your code, try to be concise and localize well all your calls

So what are your EventEmitter use cases? 
Do you dislike them?
Please share your thoughts!

you can find a repl of this code here
https://repl.it/@AngelMunoz/Naive-Event-Emmiter

and if you are interested in lightweight implementations of event emitters, take a look at this gist!
https://gist.github.com/mudge/5830382

(emit) person comes by later
**clicks**
