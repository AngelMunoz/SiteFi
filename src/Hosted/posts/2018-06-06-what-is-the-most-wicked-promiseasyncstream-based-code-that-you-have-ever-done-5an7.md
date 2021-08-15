---
title: What is the most Wicked (promise|async)/stream based code that you have ever done?
subtitle: ~
categories: node,javascript,discuss
abstract: Care to share some experiences about it?
date: 2018-06-06
language: en
---

I remember doing a naïve (I guess, I never requested a code review from someone else) implementation  of a stream which used a mongodb cursor to process thousands of registries into another database
basically I mixed streams, eventemmiters (inherited of the stream), some generators and promise based code.
