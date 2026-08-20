export type DiscoveryCardCategory = {
  readonly id: string
  readonly displayName: string
  readonly significance: string
}

export type DiscoveryCard = {
  readonly id: string
  readonly displayName: string
  readonly categoryId: string
  readonly description: string
  readonly examples: readonly string[]
  readonly microsoftServices: readonly string[]
}

export const discoveryCardCategories: readonly DiscoveryCardCategory[] =
[
  {
    "id": "agentic",
    "displayName": "Agentic",
    "significance": "Agentic solutions represent AI systems that autonomously plan, decide, and take action across multi-step workflows to achieve business outcomes with minimal human intervention."
  },
  {
    "id": "communication",
    "displayName": "Communication",
    "significance": "AI enhances interaction between humans and machines through natural language understanding."
  },
  {
    "id": "content-creation",
    "displayName": "Content Creation",
    "significance": "AI generates text, images, and synthetic data to support creativity and automation."
  },
  {
    "id": "data-and-predictive-analytics",
    "displayName": "Data And Predictive Analytics",
    "significance": "AI processes vast datasets to detect patterns, predict outcomes, and support analytics-driven decision-making."
  },
  {
    "id": "decision-making",
    "displayName": "Decision Making",
    "significance": "AI assists businesses in making faster, data-driven decisions based on predictive models."
  },
  {
    "id": "environmental-awareness",
    "displayName": "Environmental Awareness",
    "significance": "AI assists in environmental monitoring and interaction through sensory input."
  },
  {
    "id": "information-management",
    "displayName": "Information Management",
    "significance": "AI structures, retrieves, and organizes data for efficient access and analysis."
  },
  {
    "id": "navigation-and-control",
    "displayName": "Navigation And Control",
    "significance": "AI is used for robotics, autonomous navigation, and smart automation in various environments."
  },
  {
    "id": "process-optimization",
    "displayName": "Process Optimization",
    "significance": "AI enhances efficiency, cost reduction, and resource allocation across industries."
  },
  {
    "id": "speech-recognition",
    "displayName": "Speech Recognition",
    "significance": "AI interprets, transcribes, and generates human speech for accessibility and automation."
  },
  {
    "id": "task-automation",
    "displayName": "Task Automation",
    "significance": "AI streamlines repetitive tasks, reducing human effort and improving efficiency."
  },
  {
    "id": "text-processing",
    "displayName": "Text Processing",
    "significance": "AI processes, analyzes, and generates natural language text for various applications."
  },
  {
    "id": "visual-perception",
    "displayName": "Visual Perception",
    "significance": "AI enables machines to interpret and analyze images, videos, and visual data."
  }
]

export const discoveryCards: readonly DiscoveryCard[] =
[
  {
    "id": "navigation-and-control-automate-home-operations",
    "displayName": "Automate home operations",
    "categoryId": "navigation-and-control",
    "description": "AI-Controlled home automation and robotics",
    "examples": [
      "Lighting and Temperature - Control home's lighting and temperature based on user preferences and time of day.",
      "Cleaning - Operate home cleaning robots to maintain a clean living environment."
    ],
    "microsoftServices": [
      "Azure IoT Operations",
      "Azure Digital Twins",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "navigation-and-control-navigate",
    "displayName": "Navigate",
    "categoryId": "navigation-and-control",
    "description": "Guide people, ground, air and water vehicles to navigate autonomously using AI.",
    "examples": [
      "Disaster Response - Operate a walking robot to navigate through a disaster site for rescue operations.",
      "Agriculture - Drive a vehicle that can autonomously navigate a farm for tasks like seeding or harvesting."
    ],
    "microsoftServices": [
      "Azure IoT Operations",
      "Azure Edge",
      "Azure Digital Twins",
      "Azure AI Foundry",
      "Azure Maps"
    ]
  },
  {
    "id": "navigation-and-control-human-robot-interaction",
    "displayName": "Human-robot interaction",
    "categoryId": "navigation-and-control",
    "description": "Use AI to power robots that interact or collaborate safely with people.",
    "examples": [
      "Customer Service - Operate a robot that can understand and respond to customer queries.",
      "Manufacturing - Program a collaborative robot (cobot) to safely work alongside humans in a factory."
    ],
    "microsoftServices": [
      "Azure IoT Operations",
      "Azure Digital Twins",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "navigation-and-control-automate-fulfillment",
    "displayName": "Automate fulfillment",
    "categoryId": "navigation-and-control",
    "description": "Use AI to drive warehouse and supermarket systems, and control robots for efficient order fulfillment.",
    "examples": [
      "Warehouse - Automate order picking with robots programmed to navigate the layout efficiently.",
      "Fulfillment Center - Operate robots to sort, pack, and dispatch goods based on order information."
    ],
    "microsoftServices": [
      "Azure IoT Operations",
      "Azure Digital Twins",
      "Azure AI Foundry",
      "Dynamics 365 Supply Chain Management"
    ]
  },
  {
    "id": "environmental-awareness-interpret-touch-for-control",
    "displayName": "Interpret touch for control",
    "categoryId": "environmental-awareness",
    "description": "Enable intuitive control through touch-based interactions.",
    "examples": [
      "Tech sector - Navigating smartphone or tablet interfaces using touch gestures.",
      "Design sector - Drawing or annotating on touch-enabled devices for creative work."
    ],
    "microsoftServices": [
      "Microsoft Surface",
      "Azure AI Custom Vision"
    ]
  },
  {
    "id": "environmental-awareness-understand-flavors-and-tastes",
    "displayName": "Understand flavors and tastes",
    "categoryId": "environmental-awareness",
    "description": "Create and predict flavor profiles based on user purchases and new recipes.",
    "examples": [
      "Retail - Create personalized flavor profiles based on food and beverage purchases.",
      "Food Industry - Predict the optimum flavor profiles for new recipes or food products."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Microsoft Fabric"
    ]
  },
  {
    "id": "environmental-awareness-predict-chemical-properties",
    "displayName": "Predict chemical properties",
    "categoryId": "environmental-awareness",
    "description": "Forecast the smell or detect hazards based on chemical profiles.",
    "examples": [
      "Chemical Industry - Enhance fabric softener smell with optimal chemical combinations.",
      "Safety and Security - Identify hazards based on unique chemical profiles."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Microsoft Fabric"
    ]
  },
  {
    "id": "environmental-awareness-navigate-smartly-with-sensing",
    "displayName": "Navigate smartly with sensing",
    "categoryId": "environmental-awareness",
    "description": "Detect objects and environments for advanced navigation.",
    "examples": [
      "Robotics - Recognize objects and obstacles for safe, efficient robotic navigation.",
      "Retail - Understand surroundings to provide context-aware product recommendations."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Vision",
      "Azure Spatial Anchors"
    ]
  },
  {
    "id": "environmental-awareness-detect-motion",
    "displayName": "Detect motion",
    "categoryId": "environmental-awareness",
    "description": "Use motion detection for improved interaction and monitoring.",
    "examples": [
      "Sports - Monitor and analyze athlete movements for performance improvements.",
      "Security - Detect unusual motions for enhanced security surveillance and alerts."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure IoT Edge",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "process-optimization-streamline-randd",
    "displayName": "Streamline R&D",
    "categoryId": "process-optimization",
    "description": "Optimize research and development processes through intelligent AI analysis.",
    "examples": [
      "Lab Efficiency - Automate and optimize laboratory processes for increased efficiency with AI.",
      "Product Success Prediction - Predict the success of new product developments based on historical data using AI."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Microsoft Fabric",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "process-optimization-adjust-pricing",
    "displayName": "Adjust pricing",
    "categoryId": "process-optimization",
    "description": "Use AI for pricing optimization to enhance profitability.",
    "examples": [
      "Real-time Adjustments - Adjust product prices in real-time based on market demand and competition with AI.",
      "Price Prediction - Predict the optimal price for a new product or service using AI."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Microsoft Fabric",
      "Dynamics 365 Finance & Operations",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "process-optimization-improve-routing",
    "displayName": "Improve routing",
    "categoryId": "process-optimization",
    "description": "Leverage AI to efficiently optimize routes, logistics, and supply chains.",
    "examples": [
      "Delivery Optimization - Determine the most efficient delivery routes to minimize time and fuel costs using AI.",
      "Inventory Management - Optimize inventory management based on demand forecasting with AI."
    ],
    "microsoftServices": [
      "Azure Maps",
      "Microsoft 365 Copilot for Sales",
      "Dynamics 365 Supply Chain Management",
      "Azure Machine Learning",
      "Azure IoT Operations"
    ]
  },
  {
    "id": "process-optimization-enhance-farming",
    "displayName": "Enhance farming",
    "categoryId": "process-optimization",
    "description": "Use AI to optimize farming for enhanced efficiency.",
    "examples": [
      "Optimal Crop Growth - Utilize AI to analyze soil and weather data for optimal crop growth.",
      "Crop Monitoring - Monitor crops throughout their growth cycle using cameras and sensors to notify if and when intervention is required."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Microsoft Fabric",
      "Azure IoT Operations",
      "Dynamics 365 Supply Chain Management"
    ]
  },
  {
    "id": "process-optimization-enhance-production",
    "displayName": "Enhance production",
    "categoryId": "process-optimization",
    "description": "Use AI to optimize production and warehousing for enhanced efficiency.",
    "examples": [
      "Maintenance Prediction - Predict machinery maintenance needs with AI to avoid production downtime.",
      "Demand Forecast - Predict demand to optimize production, storage, and delivery of goods."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Microsoft Fabric",
      "Azure IoT Operations",
      "Dynamics 365 Supply Chain Management"
    ]
  },
  {
    "id": "process-optimization-manage-complex-systems",
    "displayName": "Manage complex systems",
    "categoryId": "process-optimization",
    "description": "Use AI to efficiently manage cities, countries, and large factories.",
    "examples": [
      "Traffic Optimization - Analyze city traffic data with AI to optimize road networks and reduce congestion.",
      "Resource Management - Use AI to manage national resources and public services efficiently."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure Digital Twins",
      "Dynamics 365 Supply Chain Management",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "process-optimization-spot-damage-predict-failure",
    "displayName": "Spot damage, predict failure",
    "categoryId": "process-optimization",
    "description": "Leverage AI for damage detection and predictive maintenance.",
    "examples": [
      "Damage Detection - Detect signs of damage in machinery for timely repairs using AI.",
      "Predictive Maintenance - Predict maintenance needs with AI to prevent unexpected equipment breakdowns."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Dynamics 365 Field Service",
      "Azure IoT Operations"
    ]
  },
  {
    "id": "process-optimization-optimize-sales-workflows",
    "displayName": "Optimize sales workflows",
    "categoryId": "process-optimization",
    "description": "Use AI for predictive insights and automation in sales workflows, streamlining tasks and CRM.",
    "examples": [
      "Lead Scoring - Evaluate past data with AI to prioritize leads, targeting promising prospects.",
      "Tailored Follow-Ups - Assess customer interactions with AI to suggest custom actions, boosting sales closure rates."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Microsoft Fabric",
      "Microsoft 365 Copilot for Sales"
    ]
  },
  {
    "id": "process-optimization-streamline-field-service",
    "displayName": "Streamline field service",
    "categoryId": "process-optimization",
    "description": "Boost technician productivity and simplify work order management through AI.",
    "examples": [
      "Field Service - Technicians receive summarized key points of work orders, enabling quicker job comprehension.",
      "Management - Efficiently schedule and manage work orders with AI assistance."
    ],
    "microsoftServices": [
      "Dynamics 365 Field Service",
      "Azure AI Language",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "process-optimization-simplify-app-development",
    "displayName": "Simplify app development",
    "categoryId": "process-optimization",
    "description": "Speed up development and enhance creativity by transforming natural language into functional app components.",
    "examples": [
      "HR - Create employee onboarding apps quickly by describing the desired functionality.",
      "Retail - Develop inventory management apps by converting sketches or designs into working prototypes."
    ],
    "microsoftServices": [
      "Power Platform - Power Apps",
      "GitHub Copilot"
    ]
  },
  {
    "id": "process-optimization-quality-control-and-maintenance",
    "displayName": "Quality control & maintenance",
    "categoryId": "process-optimization",
    "description": "Use AI for enhanced quality control and predictive maintenance.",
    "examples": [
      "Real-Time Defect Detection - Identify product defects in real-time during production processes with AI.",
      "Quality Standards Improvement - Analyze historical data with AI to improve quality control standards."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure IoT Edge",
      "Dynamics 365 Field Service",
      "Azure AI Vision",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "data-and-predictive-analytics-visualize-data",
    "displayName": "Visualize data",
    "categoryId": "data-and-predictive-analytics",
    "description": "Leverage AI to automatically visualize and interpret data relationships.",
    "examples": [
      "Sales - Use AI to generate visuals and reports, identifying customer trends and buying patterns.",
      "Business Analysis - Employ AI for swift performance reporting and visual data arrangement."
    ],
    "microsoftServices": [
      "Microsoft Fabric",
      "Azure AI Foundry",
      "Azure Machine Learning",
      "Microsoft 365 Copilot for Sales"
    ]
  },
  {
    "id": "data-and-predictive-analytics-gain-market-insights",
    "displayName": "Gain market insights",
    "categoryId": "data-and-predictive-analytics",
    "description": "Utilize AI to understand market trends, forecast, and assess competitor behavior.",
    "examples": [
      "Retail - Analyze data points to understand market trends and inform strategic decisions.",
      "Marketing - Track competitors' digital footprint to comprehend their strategies and performance."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure Machine Learning",
      "Power BI",
      "Bing Search API",
      "Azure Maps",
      "Azure AI Language"
    ]
  },
  {
    "id": "data-and-predictive-analytics-analyze-sentiments",
    "displayName": "Analyze sentiments",
    "categoryId": "data-and-predictive-analytics",
    "description": "Leverage AI to detect and analyze sentiment in text and images.",
    "examples": [
      "Customer Service - Use AI to gauge customer sentiment from feedback or product reviews.",
      "Marketing - Analyze social media comments with AI to assess public sentiment about a brand or event."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Azure AI Custom Vision",
      "Power Platform AI Builder"
    ]
  },
  {
    "id": "data-and-predictive-analytics-identify-data-patterns",
    "displayName": "Identify data patterns",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to detect patterns, connections, and associations in your data.",
    "examples": [
      "Transportation - Analyze traffic data with AI to identify congestion times and high-accident zones.",
      "Healthcare - Use AI to analyze large amounts of healthcare data to detect early signs of diseases like cancer or Alzheimer\u00e2\u20ac\u2122s."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Copilot for Power BI",
      "Microsoft Fabric",
      "Azure AI Search"
    ]
  },
  {
    "id": "data-and-predictive-analytics-detect-anomalies",
    "displayName": "Detect anomalies",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to identify unusual patterns in data streams or image data.",
    "examples": [
      "Banking - Detect suspicious credit card activities that may indicate fraud.",
      "Cybersecurity - Spot unusual traffic patterns in login data to detect potential cyber attacks."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Custom Vision",
      "Azure Sentinel"
    ]
  },
  {
    "id": "data-and-predictive-analytics-understand-customers",
    "displayName": "Understand customers",
    "categoryId": "data-and-predictive-analytics",
    "description": "Gain insights into customer behavior, predict their needs, and tailor solutions effectively.",
    "examples": [
      "Retail - Suggest products based on a customer's past purchases.",
      "Marketing - Personalize campaigns based on customers' interactions with the brand."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Dynamics 365 Customer Insights",
      "Azure AI Foundry",
      "Microsoft Fabric"
    ]
  },
  {
    "id": "data-and-predictive-analytics-predict-risk-or-fraud",
    "displayName": "Predict risk or fraud",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to detect suspicious activities and predict potential fraud risks in real-time.",
    "examples": [
      "Banking - Identify suspicious patterns in transactions to detect potential fraud.",
      "Cybersecurity - Predict threats in real-time to prevent data breaches."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Microsoft Fabric",
      "Azure Machine Learning",
      "Dynamics 365 Fraud Protection"
    ]
  },
  {
    "id": "data-and-predictive-analytics-forecast-events-and-outcomes",
    "displayName": "Forecast events & outcomes",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to predict future scenarios and outcomes based on historical data.",
    "examples": [
      "Project Management - Estimate project completion time based on past performance data.",
      "Finance - Predict market trends to shape investment strategies."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Microsoft Fabric"
    ]
  },
  {
    "id": "data-and-predictive-analytics-predict-customer-churn",
    "displayName": "Predict customer churn",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to anticipate customer churn and respond proactively to retain them.",
    "examples": [
      "Subscription Services - Predict potential cancellations based on customer usage patterns.",
      "Marketing - Offer personalized incentives to customers at risk of churning."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Microsoft Fabric",
      "Dynamics 365 Customer Insights"
    ]
  },
  {
    "id": "data-and-predictive-analytics-predict-and-plan-demand",
    "displayName": "Predict & plan demand",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to analyze sales data, identify patterns, and forecast demand accurately.",
    "examples": [
      "Sales - Analyze historical sales data to identify patterns and seasonality.",
      "Inventory Management - Plan inventory based on forecasted demand to avoid stockouts and overstock."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Microsoft Fabric",
      "Dynamics 365 Supply Chain Management",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "data-and-predictive-analytics-simplify-data-analysis",
    "displayName": "Simplify data analysis",
    "categoryId": "data-and-predictive-analytics",
    "description": "Use AI to create complex data queries and dashboards through natural language, making insights more accessible.",
    "examples": [
      "Sales - Generate expressions for trend analysis by describing the analytical goal.",
      "Customer Service - Build a dashboard summarizing customer feedback using natural language queries."
    ],
    "microsoftServices": [
      "Copilot for Power BI",
      "Azure AI Language",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "decision-making-spot-anomalies",
    "displayName": "Spot anomalies",
    "categoryId": "decision-making",
    "description": "Leverage AI to find unusual data patterns or outliers, allowing for early issue detection and proactive solutions.",
    "examples": [
      "Manufacturing - Monitor equipment data to spot anomalies, enable proactive maintenance, and avoid costly breakdowns.",
      "Healthcare - Detect anomalies in patient health data for early detection of potential health issues."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "decision-making-moderate-content",
    "displayName": "Moderate content",
    "categoryId": "decision-making",
    "description": "Use AI to automatically detect and filter inappropriate or harmful content, ensuring a safe digital environment.",
    "examples": [
      "E-commerce - Review product listings and user reviews for potentially fraudulent or inappropriate content.",
      "Social Media - Filter offensive language, images, and videos to maintain a safe online community."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure AI Content Safety",
      "Azure AI Custom Vision",
      "Azure AI Vision"
    ]
  },
  {
    "id": "decision-making-enhance-decisions",
    "displayName": "Enhance decisions",
    "categoryId": "decision-making",
    "description": "Use AI to assist in complex decision making, optimize processes, and predict outcomes, boosting efficiency and effectiveness.",
    "examples": [
      "Retail - Predict sales trends and optimize inventory management for business efficiency.",
      "Healthcare - Aid clinical decisions by predicting patient outcomes based on historical data."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure Machine Learning",
      "Dynamics 365 Customer Insights"
    ]
  },
  {
    "id": "decision-making-personalize-content",
    "displayName": "Personalize content",
    "categoryId": "decision-making",
    "description": "Leverage AI to generate tailored content suggestions based on user behavior, boosting customer engagement and experience.",
    "examples": [
      "E-commerce - Personalize product suggestions based on user browsing history, purchasing behavior, and satisfaction.",
      "Streaming Services - Offer tailored movie or music recommendations based on user preferences, improving user experience."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Dynamics 365 Customer Insights",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "decision-making-boost-sales-with-insights",
    "displayName": "Boost sales with insights",
    "categoryId": "decision-making",
    "description": "Leverage CRM data with AI to enhance sales strategies and customer relationships.",
    "examples": [
      "Marketing - Draft personalized sales emails using data insights for improved engagement.",
      "Sales - Summarize customer meetings and auto-update records, ensuring data accuracy."
    ],
    "microsoftServices": [
      "Dynamics 365 Sales",
      "Microsoft 365 Copilot for Sales",
      "Microsoft 365 Copilot for Service",
      "Dynamics 365 Customer Insights"
    ]
  },
  {
    "id": "decision-making-streamline-financial-processes",
    "displayName": "Streamline financial processes",
    "categoryId": "decision-making",
    "description": "Utilize AI to automate financial tasks, improving accuracy and efficiency.",
    "examples": [
      "E-commerce - Auto-generate detailed product descriptions for online catalogs.",
      "Banking - Assess credit risks by analyzing customer financial data."
    ],
    "microsoftServices": [
      "Dynamics 365 Finance & Operations",
      "Azure AI Language"
    ]
  },
  {
    "id": "decision-making-refine-processes",
    "displayName": "Refine processes",
    "categoryId": "decision-making",
    "description": "Leverage AI\u00e2\u20ac\u2122s ability to learn from past experiences and enhance actions, leading to better performance and efficiency.",
    "examples": [
      "Manufacturing - Use AI for predictive maintenance, minimize downtime, and reduce costs.",
      "Education - Employ AI to adapt to students\u00e2\u20ac\u2122 learning styles, improving educational outcomes."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "decision-making-optimize-strategy",
    "displayName": "Optimize strategy",
    "categoryId": "decision-making",
    "description": "Use AI to predict and execute proactive strategies, optimizing results and boosting efficiency.",
    "examples": [
      "Sales & Marketing - Predict the optimal strategy or sales approach for each customer, increasing conversions.",
      "Customer Service - Use AI to suggest the best response to customer inquiries, enhancing satisfaction and retention."
    ],
    "microsoftServices": [
      "Microsoft 365 Copilot for Sales",
      "Microsoft 365 Copilot for Service",
      "Azure Machine Learning"
    ]
  },
  {
    "id": "decision-making-resolve-complex-issues",
    "displayName": "Resolve complex issues",
    "categoryId": "decision-making",
    "description": "Use AI to analyze vast data, forecast outcomes, and provide optimized solutions to intricate problems.",
    "examples": [
      "Supply Chain - Enhance logistics and inventory management by predicting demand and identifying bottlenecks.",
      "Cybersecurity - Employ AI to detect potential security threats, enabling proactive defense strategies."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry",
      "Dynamics 365 Supply Chain Management"
    ]
  },
  {
    "id": "task-automation-help-with-admin-tasks",
    "displayName": "Help with admin tasks",
    "categoryId": "task-automation",
    "description": "Use AI to automate repetitive and time-consuming office tasks.",
    "examples": [
      "Finance - Automate data entry and report generation for increased efficiency.",
      "HR - Intelligently schedule meetings based on everyone's availability."
    ],
    "microsoftServices": [
      "Microsoft Copilot Studio",
      "Microsoft 365 Copilot",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "task-automation-simplify-household-tasks",
    "displayName": "Simplify household tasks",
    "categoryId": "task-automation",
    "description": "Use AI to automate and simplify everyday household tasks.",
    "examples": [
      "Smart Home - Control lights, thermostats, and locks through voice commands.",
      "Energy Management - Monitor and regulate appliance energy usage for optimized consumption."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Microsoft 365 Agents SDK",
      "Power Platform: AI Builder"
    ]
  },
  {
    "id": "task-automation-help-with-personal-tasks",
    "displayName": "Help with personal tasks",
    "categoryId": "task-automation",
    "description": "Utilize AI to assist with daily personal tasks and health management.",
    "examples": [
      "Health & Fitness - Get personalized fitness and health recommendations based on lifestyle and goals.",
      "Personal Reminders - Set reminders for appointments, medication intake, or bill payments."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Microsoft 365 Copilot"
    ]
  },
  {
    "id": "task-automation-workflow-creation",
    "displayName": "Workflow creation",
    "categoryId": "task-automation",
    "description": "Use AI to create the steps for a workflow, enhancing efficiency.",
    "examples": [
      "Finance - Automate expense approval processes by letting AI interpret descriptive sentences.",
      "Customer Service - Streamline responses by generating automation flows from common inquiries."
    ],
    "microsoftServices": [
      "Microsoft Copilot Studio",
      "Azure AI Language",
      "Microsoft 365 Copilot",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "visual-perception-recognize-and-understand-forms",
    "displayName": "Recognize and understand forms",
    "categoryId": "visual-perception",
    "description": "Use AI to extract data from various forms like receipts, invoices, and standard government or business documents.",
    "examples": [
      "Handwritten Text - AI can identify handwritten text along with printed text.",
      "Custom Models - Train custom AI models to recognize specific types of forms and documents."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure AI Document Intelligence",
      "Power Platform: AI Builder",
      "Azure AI Content Understanding"
    ]
  },
  {
    "id": "visual-perception-identify-objects",
    "displayName": "Identify objects",
    "categoryId": "visual-perception",
    "description": "Use AI to create custom image recognition models for specific needs and applications.",
    "examples": [
      "Accessibility - Assist visually impaired users with descriptions of environments.",
      "Retail - Automatically identify and catalog products in images for streamlined inventory management."
    ],
    "microsoftServices": [
      "Azure AI Vision",
      "Azure AI Foundry",
      "Azure Machine Learning"
    ]
  },
  {
    "id": "visual-perception-convert-images-to-text",
    "displayName": "Convert images to text",
    "categoryId": "visual-perception",
    "description": "Use AI to transform various types of documents into editable and searchable data.",
    "examples": [
      "Administration - Convert printed documents into digital text for easy search and retrieval.",
      "Education - Transform printed textbooks into digital format, facilitating remote learning."
    ],
    "microsoftServices": [
      "Azure AI Vision",
      "Power Platform: AI Builder",
      "Bing Search API"
    ]
  },
  {
    "id": "visual-perception-identify-faces",
    "displayName": "Identify faces",
    "categoryId": "visual-perception",
    "description": "Use AI to identify or verify a person's identity by comparing and analyzing patterns based on facial contours.",
    "examples": [
      "Security Systems - Enhance security by enabling systems to identify authorized individuals for access control.",
      "Social Media - Enables automatic tagging of individuals in social media platforms."
    ],
    "microsoftServices": [
      "Azure AI Face Service",
      "Azure AI Foundry",
      "Azure AI Vision"
    ]
  },
  {
    "id": "visual-perception-understand-environments",
    "displayName": "Understand environments",
    "categoryId": "visual-perception",
    "description": "Use AI to interpret relationships, patterns, and trends between different locations or spaces.",
    "examples": [
      "Retail - Analyze customer movements and interactions within the store to understand behavior.",
      "Urban Planning - Optimize infrastructure and services by analyzing spatial data like population density or traffic patterns."
    ],
    "microsoftServices": [
      "Azure Maps",
      "Azure Digital Twins",
      "Azure AI Vision",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "visual-perception-analyze-images",
    "displayName": "Analyze images",
    "categoryId": "visual-perception",
    "description": "Utilize AI to understand, interpret, and derive insights from visual data.",
    "examples": [
      "Agriculture - Assist farmers in crop monitoring by analyzing drone-captured images of farmland for signs of disease or distress.",
      "Traffic Control - Manage traffic by analyzing real-time road images to identify congestion, accidents, or violations."
    ],
    "microsoftServices": [
      "Azure AI Vision",
      "Azure AI Foundry",
      "Azure Machine Learning",
      "Bing Search API",
      "Azure AI Content Understanding"
    ]
  },
  {
    "id": "visual-perception-categorize-images",
    "displayName": "Categorize images",
    "categoryId": "visual-perception",
    "description": "Use AI to tag images based on their content, simplifying management and retrieval of visual data.",
    "examples": [
      "Digital Asset Management - Simplify management of large image libraries by tagging images for easy retrieval.",
      "E-Commerce - Enhance customer experience by tagging product images, enabling customers to search for similar products."
    ],
    "microsoftServices": [
      "Azure AI Vision",
      "Azure AI Foundry",
      "Azure Machine Learning",
      "Azure AI Content Understanding"
    ]
  },
  {
    "id": "visual-perception-create-image-captions",
    "displayName": "Create image captions",
    "categoryId": "visual-perception",
    "description": "Use AI to generate textual descriptions of images, enhancing understanding without visual perception.",
    "examples": [
      "Accessibility - Improve accessibility for visually impaired users with textual descriptions of images on websites or applications.",
      "Education - Assist learning by providing detailed captions for educational images or diagrams."
    ],
    "microsoftServices": [
      "Azure AI Vision"
    ]
  },
  {
    "id": "visual-perception-generate-image-metadata",
    "displayName": "Generate image metadata",
    "categoryId": "visual-perception",
    "description": "Use AI to create structured data about images, enhancing searchability and management.",
    "examples": [
      "Digital Archives - Facilitate search and retrieval in large image archives by generating detailed metadata for each image.",
      "Photography - Assist photographers in managing their portfolios by creating metadata for each photograph, including details like location, subject, and camera settings."
    ],
    "microsoftServices": [
      "Azure AI Vision",
      "Azure AI Foundry",
      "Azure Machine Learning"
    ]
  },
  {
    "id": "text-processing-analyze-emotion-and-sentiment",
    "displayName": "Analyze emotion and sentiment",
    "categoryId": "text-processing",
    "description": "Use AI to detect and analyze user sentiment from text, helping to improve services and customer interactions.",
    "examples": [
      "Customer Support - Analyze customer feedback or complaints with AI, identifying negative sentiments to prioritize actions and improve satisfaction.",
      "Market Research - Understand customer sentiments from social media posts or product reviews, providing insights into public opinion and brand perception."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure AI Foundry",
      "Microsoft Copilot Studio",
      "Microsoft 365 Agents SDK",
      "Azure Machine Learning"
    ]
  },
  {
    "id": "text-processing-generate-contextual-text",
    "displayName": "Generate contextual text",
    "categoryId": "text-processing",
    "description": "Use AI to generate contextually relevant and personalized text, enhancing user experience and productivity.",
    "examples": [
      "Content Creation - Create drafts for articles, blogs, or social media posts with AI to aid creation and curation.",
      "Email Composition - Suggest email responses or draft emails based on past interactions with AI, saving time and effort."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure AI Foundry",
      "Microsoft Copilot Studio",
      "Microsoft 365 Agents SDK",
      "Microsoft 365 Copilot"
    ]
  },
  {
    "id": "text-processing-summarize-text",
    "displayName": "Summarize text",
    "categoryId": "text-processing",
    "description": "Use AI to extract key points from large text data, aiding in quick understanding and efficient information retrieval.",
    "examples": [
      "Business Reports - Summarize lengthy business reports into key insights with AI, enabling swift decision-making.",
      "News Digest - Summarize news articles into key points with AI, enabling readers to quickly grasp the main information."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure AI Foundry",
      "Microsoft Copilot Studio",
      "Microsoft 365 Agents SDK",
      "Microsoft Teams Premium"
    ]
  },
  {
    "id": "text-processing-translate-text",
    "displayName": "Translate text",
    "categoryId": "text-processing",
    "description": "Use AI for translation across multiple languages, facilitating global communication and breaking language barriers.",
    "examples": [
      "International Business - Translate business documents or emails instantly with AI, enabling smooth communication in multinational companies.",
      "Education - Translate educational materials with AI, providing access to diverse learning resources and aiding in multilingual education."
    ],
    "microsoftServices": [
      "Azure AI Translator",
      "Azure AI Foundry",
      "Microsoft Copilot Studio",
      "Microsoft Teams Premium"
    ]
  },
  {
    "id": "communication-engage-in-natural-conversations",
    "displayName": "Engage in natural conversations",
    "categoryId": "communication",
    "description": "Use AI to facilitate natural and engaging conversations, enhancing user experience and interaction.",
    "examples": [
      "Customer Support - Understand and respond to user queries in natural language through an AI-powered chatbot.",
      "Voice Assistants - Engage in voice-based interactions with users for hands-free operations using AI."
    ],
    "microsoftServices": [
      "Azure AI Bot Service",
      "Azure AI Language",
      "Azure AI Translator",
      "Microsoft Copilot Studio",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "communication-convert-text-into-speech",
    "displayName": "Convert text into speech",
    "categoryId": "communication",
    "description": "Use AI to transform text into lifelike speech, enhancing accessibility and user experience.",
    "examples": [
      "Accessibility - Read out articles, books, or documents with AI for users with visual impairments.",
      "Voice Assistants - Provide voice-based responses or instructions in applications or devices using AI."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "communication-automate-answers",
    "displayName": "Automate answers",
    "categoryId": "communication",
    "description": "Quickly address inquiries with instant, precise responses.",
    "examples": [
      "Retail - Handle customer queries about products via chatbot.",
      "Travel - Answer queries from airport guests on services and navigation."
    ],
    "microsoftServices": [
      "Azure AI Search",
      "Azure AI Foundry",
      "Azure AI Bot Service",
      "Microsoft Copilot Studio",
      "Microsoft 365 Agents SDK"
    ]
  },
  {
    "id": "communication-translate-speech-instantly",
    "displayName": "Translate speech instantly",
    "categoryId": "communication",
    "description": "Enable instant translation of spoken language for seamless communication.",
    "examples": [
      "Customer Support - Provide real-time translation in multilingual customer interactions.",
      "Travel & Tourism - Facilitate communication for travelers by translating local languages instantly."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure AI Language",
      "Microsoft 365 Copilot"
    ]
  },
  {
    "id": "communication-communicate-via-avatar",
    "displayName": "Communicate via avatar",
    "categoryId": "communication",
    "description": "Create engaging experiences with audio and visual avatars for immersive gaming and personal assistants.",
    "examples": [
      "Gaming Industry - Enhance user immersion with unique voiced characters.",
      "Virtual Assistants - Improve user engagement with lifelike, expressive voices."
    ],
    "microsoftServices": [
      "Azure AI Foundry"
    ]
  },
  {
    "id": "communication-understand-user-intent",
    "displayName": "Understand user intent",
    "categoryId": "communication",
    "description": "Interpret human language to identify users' specific intentions and context.",
    "examples": [
      "Customer Service - Understand customer requests and provide accurate support.",
      "Chatbots - Enable chatbots to understand queries and provide relevant responses."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure AI Bot Service",
      "Azure AI Foundry",
      "Microsoft 365 Agents SDK",
      "Microsoft Copilot Studio"
    ]
  },
  {
    "id": "content-creation-generate-images-from-text",
    "displayName": "Generate images from text",
    "categoryId": "content-creation",
    "description": "Create unique images from textual descriptions utilizing AI.",
    "examples": [
      "Entertainment & Media - Create unique characters and scenes for movies or games from text descriptions.",
      "Advertising - Generate visuals for ad campaigns based on text-described themes or concepts."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Microsoft Designer",
      "Microsoft Copilot",
      "Azure AI Content Understanding"
    ]
  },
  {
    "id": "content-creation-generate-or-enhance-text",
    "displayName": "Generate or enhance text",
    "categoryId": "content-creation",
    "description": "Implement advanced natural language understanding for more sophisticated, human-like interactions.",
    "examples": [
      "Content Creation - Generate high-quality text for blogs, articles, or social media posts, enhancing productivity.",
      "Data Analysis - Extract insights from large volumes of text data, enabling informed business decisions."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure AI Language",
      "Microsoft Copilot"
    ]
  },
  {
    "id": "content-creation-generate-synthetic-data",
    "displayName": "Generate synthetic data",
    "categoryId": "content-creation",
    "description": "Create synthetic data that mimics real data for robust model training without compromising privacy.",
    "examples": [
      "Finance - Simulate financial scenarios to test models, enhancing risk management without revealing sensitive data.",
      "Retail - Generate synthetic customer behavior data to optimize sales strategies while preserving customer privacy."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure Machine Learning",
      "Microsoft Fabric"
    ]
  },
  {
    "id": "content-creation-personalize-marketing",
    "displayName": "Personalize marketing",
    "categoryId": "content-creation",
    "description": "Use AI to tailor marketing campaigns by analyzing customer data and predicting optimal engagement strategies.",
    "examples": [
      "Targeted Campaigns - Segment customers based on behavior and preferences for impactful marketing campaigns with AI.",
      "Content Improvement - Leverage AI to suggest enhancements to marketing content, aligning with target audience interests."
    ],
    "microsoftServices": [
      "Azure AI Search",
      "Azure Machine Learning",
      "Dynamics 365 Customer Insights"
    ]
  },
  {
    "id": "content-creation-create-dynamic-web-pages",
    "displayName": "Create dynamic web pages",
    "categoryId": "content-creation",
    "description": "Use AI to generate text, forms, and layouts for web pages, simplifying web development.",
    "examples": [
      "Form Creation - Use AI to automatically generate detailed forms for event registrations.",
      "Dynamic Layouts - Create dynamic webpage layouts for a product catalog using AI."
    ],
    "microsoftServices": [
      "Power Platform: Power Pages",
      "Azure AI Language",
      "GitHub Copilot"
    ]
  },
  {
    "id": "speech-recognition-convert-speech-to-text",
    "displayName": "Convert speech to text",
    "categoryId": "speech-recognition",
    "description": "Transcribe spoken language into text with AI and enable further text-based use.",
    "examples": [
      "Transcription Services - Automate transcription of interviews, meetings, or lectures.",
      "Voice Assistants - Convert spoken commands into text for processing."
    ],
    "microsoftServices": [
      "Azure AI Foundry"
    ]
  },
  {
    "id": "speech-recognition-identify-voices",
    "displayName": "Identify voices",
    "categoryId": "speech-recognition",
    "description": "Use unique voice characteristics to enhance security and personalization.",
    "examples": [
      "Customer Service - Identify callers for personalized interactions and swift verification.",
      "Smart Home Devices - Customize user experience by recognizing different household members' voices."
    ],
    "microsoftServices": [
      "Azure AI Foundry"
    ]
  },
  {
    "id": "speech-recognition-activate-with-keyword",
    "displayName": "Activate with keyword",
    "categoryId": "speech-recognition",
    "description": "Personalize activation of AI assistants or IoT devices using specific keywords.",
    "examples": [
      "AI Assistants - Allow users to activate assistants with preferred catchphrases.",
      "Automotive Industry - Improve driving experience with custom voice command activation."
    ],
    "microsoftServices": [
      "Azure IoT Edge",
      "Azure AI Foundry",
      "Azure AI Language"
    ]
  },
  {
    "id": "speech-recognition-enable-voice-commands",
    "displayName": "Enable voice commands",
    "categoryId": "speech-recognition",
    "description": "Interact with devices or applications hands-free for convenience and accessibility.",
    "examples": [
      "Healthcare Industry - Assist medical professionals in accessing information or controlling equipment hands-free.",
      "Smart Home Appliances - Control home appliances like lights or thermostats through voice commands."
    ],
    "microsoftServices": [
      "Azure AI Language",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "speech-recognition-understand-special-context",
    "displayName": "Understand special context",
    "categoryId": "speech-recognition",
    "description": "Tailor speech synthesis and recognition for specific contexts, accents, or industries.",
    "examples": [
      "Telecommunications - Train voice assistants to understand industry-specific jargon and accents.",
      "Healthcare Industry - Enable medical applications to understand and generate medical terminologies."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure AI Language"
    ]
  },
  {
    "id": "speech-recognition-mimic-specific-voices",
    "displayName": "Mimic specific voices",
    "categoryId": "speech-recognition",
    "description": "Create lifelike, synthesized speech that mirrors individual vocal characteristics.",
    "examples": [
      "Entertainment Industry - Create realistic voiceovers for animated characters or digital influencers.",
      "Accessibility - Develop assistive technologies that speak in familiar voices for users with special needs."
    ],
    "microsoftServices": [
      "Azure AI Foundry",
      "Azure AI Language"
    ]
  },
  {
    "id": "information-management-extract-information",
    "displayName": "Extract information",
    "categoryId": "information-management",
    "description": "Use AI for efficient and speedy extraction of vital data from large databases or unsorted data stores, enhancing research and productivity.",
    "examples": [
      "Legal and Tax - Use AI to quickly locate pertinent cases, laws, and regulations from extensive legal databases.",
      "Scientific Research - Implement AI to swiftly sift through scholarly articles and extract key findings."
    ],
    "microsoftServices": [
      "Bing Search API",
      "Azure AI Search",
      "Azure AI Foundry",
      "Microsoft 365 Copilot",
      "Azure Machine Learning"
    ]
  },
  {
    "id": "information-management-retrieve-information",
    "displayName": "Retrieve information",
    "categoryId": "information-management",
    "description": "Enhance productivity by using AI to find relevant information quickly.",
    "examples": [
      "E-Commerce - Analyze user behavior and preferences to deliver precise personalized product recommendations.",
      "Learning - Assist students by finding specific information to help learn and study a concept."
    ],
    "microsoftServices": [
      "Bing Search API",
      "Azure AI Search",
      "Azure Machine Learning",
      "SharePoint Premium",
      "Microsoft 365 Copilot"
    ]
  },
  {
    "id": "information-management-organize-data",
    "displayName": "Organize data",
    "categoryId": "information-management",
    "description": "Use AI to efficiently categorize and organize data, enhancing analysis and decision-making.",
    "examples": [
      "Healthcare - Categorize patient data by symptoms, diagnosis, and treatment to aid in research and patient care.",
      "Marketing - Identify and group customers to target marketing efforts effectively based on their interests, shopping habits, or annual spending."
    ],
    "microsoftServices": [
      "Azure AI Document Intelligence",
      "Azure AI Search",
      "Azure Machine Learning",
      "SharePoint Premium"
    ]
  },
  {
    "id": "information-management-discover-patterns",
    "displayName": "Discover patterns",
    "categoryId": "information-management",
    "description": "Harness AI to identify patterns and relationships in data, leading to insightful analysis and decision-making.",
    "examples": [
      "Marketing - Employ AI to group customers based on purchasing behavior, demographics, and preferences for targeted marketing.",
      "Social Media - Use AI to identify and group trending sentiments or topics, aiding sentiment analysis and trend prediction."
    ],
    "microsoftServices": [
      "Azure Machine Learning",
      "Azure AI Foundry"
    ]
  },
  {
    "id": "information-management-structure-raw-data",
    "displayName": "Structure raw data",
    "categoryId": "information-management",
    "description": "Leverage AI to transform raw, unstructured data into a structured format for easier analysis and insights extraction.",
    "examples": [
      "Text Mining - Convert unstructured text from social media, emails, or reviews into structured data for sentiment analysis or trend identification.",
      "Healthcare - Organize disparate, unstructured patient data into structured records, enhancing healthcare delivery and research."
    ],
    "microsoftServices": [
      "Azure AI Document Intelligence",
      "Azure Machine Learning",
      "Azure AI Foundry"
    ]
  }
]

/** Shared by the facilitator's card browser and the participant join view, so both filter the same way. */
export function filterDiscoveryCards(
  cards: readonly DiscoveryCard[],
  categoryId: string,
  search: string,
): readonly DiscoveryCard[] {
  const query = search.trim().toLowerCase()
  return cards.filter((card) => {
    if (categoryId !== '' && card.categoryId !== categoryId) return false
    if (query === '') return true
    return (
      card.displayName.toLowerCase().includes(query) || card.description.toLowerCase().includes(query)
    )
  })
}
