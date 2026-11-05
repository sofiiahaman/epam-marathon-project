@group @l2
Feature: Submit group pre-order

  The owner submits the confirmed guest baskets as one consolidated order.

  Rule: Submitted baskets become one order

    @ui
    Scenario: Owner successfully submits the group pre-order
      Given the owner has submitted guest baskets for the group
      And the owner has opened the group pre-order
      When the owner submits the group pre-order
      Then the owner sees submitted group items

    @api
    Scenario: Identical dishes from guests are consolidated
      Given the owner has two submitted guests with the same dish
      When the owner submits the group pre-order through the API
      Then the submitted dish quantity is 3

    @api
    Scenario: An unconfirmed basket is not included
      Given the group has no submitted baskets
      When the owner submits the group pre-order through the API
      Then the group submit response has status 400
      And the group submit response contains "No submitted guest sessions were found."

    @api
    Scenario: The waiter sees the order confirmed by the owner
      Given the owner has submitted guest baskets for the group
      When the owner submits the group pre-order through the API
      Then the waiter order contains the submitted group dishes

  Rule: Invalid group submissions are rejected

    @api
    Scenario Outline: Group submit validates its required data
      Given <setup>
      When <action>
      Then the group submit response has status <status>
      And the group submit response contains "<message>"

      Examples:
        | setup                              | action                                      | status | message                                      |
        | the submitted baskets contain no dishes | the owner submits the group pre-order through the API | 400 | Submitted guest sessions contain no dishes. |
        | the owner has submitted guest baskets for the group | the group submit request uses a missing reservation | 404 | Reservation 00000000-0000-0000-0000-000000000000 was not found. |
        | the owner has submitted guest baskets for the group | the group submit request uses a missing group | 404 | Group reservation was not found. |

    @api
    Scenario: Submitting a group without an order is rejected
      Given the owner has submitted guest baskets for the group
      When the owner submits after the order was removed
      Then the group submit response has status 400
      And the group submit response contains "Order was not found for this reservation."

    @api
    Scenario: A group cannot be submitted twice
      Given the owner has submitted guest baskets for the group
      When the owner submits the group pre-order through the API
      And the group is submitted again
      Then the group submit response has status 400

    @api
    Scenario Outline: Only the owner can submit the group
      Given the owner has submitted guest baskets for the group
      When <caller> submits the group pre-order
      Then the group submit response has status <status>

      Examples:
        | caller             | status |
        | another customer   | 403    |
        | a guest             | 403    |
        | an anonymous user  | 401    |
