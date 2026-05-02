# TasteBudz Demo User Flow Script

Target length: 10-15 minutes
Practice target: 12 minutes

## Demo Goal

Show TasteBudz as a smooth two-user story:

1. A new user creates an account, sets up a profile, explores discovery, and creates a group event.
2. Brooke logs in as an existing participant, sees dashboard activity, joins, chats, views feedback, and uses safety/support tools.

The first user should feel like the creator. Brooke should feel like the participant. Avoid repeating the same feature from both accounts unless it proves the multi-user flow.

## Brooke Demo Credentials

- Username: `tb_demo_brooke`
- Password: `TasteBudz123!`

## Opening

Say:

> Today I will walk through the main TasteBudz MVP flow. TasteBudz helps people find compatible dining partners, create small dining events, coordinate in groups, and use safety tools when needed.
>
> I will show this from two perspectives. First, I will create a brand-new user and show the creator flow. Then I will log in as Brooke, an existing demo user, to show the participant side.

## Part 1: New User Creator Flow

Estimated time: 6-8 minutes

### 1. Create New Account

Action:

- Create a new account.
- Use a simple demo username and password.

Say:

> I am starting as a brand-new user. Registration is intentionally simple, so the user can get into the app quickly.

### 2. Onboarding and Profile Setup

Action:

- Complete onboarding/profile fields.
- Add display name, bio/social goal, ZIP code, cuisine preferences, dietary flags, and availability if shown.

Say:

> The onboarding page is important because TasteBudz uses this information for discovery and dining compatibility. The user adds basic profile details, food preferences, and social intent.
>
> Some information can appear on public profile cards, like cuisine interests or social goal. More private information, such as allergies or availability, stays controlled by the app rules.

### 3. Discover and Match People

Action:

- Open people discovery or swipe.
- Like or pass one or two candidates.
- If a Budz connection appears, show it briefly.

Say:

> Now the user can discover people. The swipe flow keeps this lightweight: I can quickly like or pass based on profile details, cuisine overlap, and social goal.
>
> If two users like each other, TasteBudz creates a Budz connection. That gives users a social starting point before planning a meal.

### 4. Quick Event Search

Action:

- Open event search or browse.
- Search or filter for open events with available seats.
- Open one event detail, but do not spend long here.

Say:

> Before creating anything new, I can check whether there is already an event that fits. This is the quick event search flow.
>
> The user can see event details such as restaurant, time, participants, and available capacity. If an event works, they could join directly.
>
> For this demo, I will create a new plan instead, so we can see the host and group-event workflow.

### 5. Communities Overview

Action:

- Open communities/groups.
- Briefly show the list or browse page.

Say:

> Communities are persistent groups for recurring dining interests. A group is different from an event: the group can continue over time, while an event is one specific dining plan.

Important:

- Do not join an existing community with the first user.
- Joining a community is reserved for Brooke in Part 2.

### 6. Create New Group

Action:

- Create a new public group.
- Use a simple name, description, and visibility.

Say:

> Now I am creating a new group. This represents the creator side of the product: a user can start a community around a dining interest or recurring meetup.

### 7. Create Group Event

Action:

- Create an event from the group context if available.
- Choose restaurant, date/time, capacity, and open/closed type.
- Save the event.

Say:

> From the group, I can create a group event. This turns the community into an actual dining plan.
>
> The event includes the restaurant, time, capacity, and visibility. The backend controls important lifecycle rules like capacity and participation, so users cannot overfill the event from the client.

### Part 1 Transition

Say:

> That completes the creator side. The new user created a profile, explored people and events, created a community, and created a group event.
>
> Now I will switch to Brooke to show the participant side of the same product.

## Part 2: Brooke Participant Flow

Estimated time: 5-7 minutes

### 1. Log In as Brooke

Action:

- Log out from the new user.
- Log in with Brooke's demo credentials.

Say:

> I am now logging in as Brooke using the existing demo account `tb_demo_brooke`. This account already has dashboard activity, notifications, groups, events, chat, and safety history, so it shows the app after it has real usage.

### 2. Show Brooke's Dashboard

Action:

- Open Brooke's dashboard.
- Point out profile summary, events, groups, and Budz.

Say:

> Brooke's dashboard is richer because she is an existing user. It gives her a quick summary of her events, groups, and Budz connections.
>
> This is why I saved the dashboard deep dive for the second account: it is more meaningful when the account already has activity.

### 3. Show Notifications

Action:

- Open notifications.
- Show unread/read activity.

Say:

> Notifications keep important changes visible inside the app. For the MVP, notifications are in-app, so users can review updates without relying on email or push notifications.

### 4. Find or Open the Community

Action:

- Find/open the group created in Part 1, or use an existing seeded group if needed.

Say:

> Now Brooke can find the community from the participant side. This proves the group is not just a static page created by the first user; another user can discover it and interact with it.

### 5. Join the Community

Action:

- Join the group/community.

Say:

> Brooke joins the community. This is intentionally shown only in the second part, so the first user creates the group and Brooke participates in it.

### 6. Join the Event

Action:

- Open the group event or another visible event.
- Join the event.

Say:

> Brooke can also join the event. This shows the participant side of the event workflow, including the capacity-controlled join behavior.

### 7. Use Group or Event Chat

Action:

- Open group or event chat.
- Read or send one short message.

Say:

> Once Brooke is a current group member or event participant, she can use chat. Chat access is tied to membership or participation, so users do not get access to conversations they are not part of.
>
> This supports practical coordination around the meal, such as timing, arrival details, and planning.

### 8. Show Completed-Event Feedback and Ratings

Action:

- Open completed-event feedback or seeded ratings.

Say:

> After an event is completed, participants can leave feedback. I am showing this from existing demo data because a newly created event should not immediately have ratings.

### 9. Show Safety and Support Briefly

Action:

- Briefly show block, report, and support chat.
- Do not walk through every moderation detail unless asked.

Say:

> TasteBudz also includes safety tools. A user can block someone, submit a report, and contact support.
>
> I am keeping this section short, but it is important to the MVP because the app is not only about discovery. It also needs trust, moderation, and support.

## Closing

Say:

> The full MVP flow is: create a profile, discover compatible people and events, form a community, create or join dining events, coordinate through chat, and use feedback and safety tools after the interaction.
>
> The two-account demo shows both sides of the product: the person creating the dining plan and the person responding to it.

## Feature Split

### First User Only

- Create account
- Onboarding/profile setup
- Discover/match people
- Quick event search
- Communities overview
- Create group
- Create group event

### Brooke Only

- Existing-account login
- Dashboard
- Notifications
- Join community
- Join event
- Chat
- Completed-event feedback/ratings
- Block/report/support

### Intentional Shared Surface

Groups and events appear in both parts, but for different reasons:

- First user creates the group/event.
- Brooke joins and participates in the group/event.

This overlap is intentional because it proves the app works across users.

## Time Control

If time is running long, cut in this order:

1. Detailed dashboard explanation
2. Extra swipe examples
3. Detailed group fields
4. Full support chat walkthrough
5. Full report form walkthrough

Keep these no matter what:

1. New account/profile
2. Discovery or swipe
3. Quick event search
4. Create group event
5. Brooke dashboard
6. Brooke joins group/event
7. Chat
8. Safety mention
