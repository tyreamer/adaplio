// Progress Ladder JavaScript Module
window.progressLadder = {
    dotNetRef: null,
    animationQueue: [],
    celebrationActive: false,

    // Initialize the progress ladder component
    initialize: function (dotNetObjectReference) {
        this.dotNetRef = dotNetObjectReference;
        this.setupAnimations();
        console.log('Progress Ladder initialized');
    },

    // Setup CSS animations and observers
    setupAnimations: function () {
        // Add intersection observer for reveal animations
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                }
            });
        }, {
            threshold: 0.1
        });

        // Observe all tier rows
        document.querySelectorAll('.tier-row').forEach(tier => {
            observer.observe(tier);
        });

        // Add physics-based easing for progress marker
        this.setupProgressMarkerPhysics();
    },

    // Update progress with smooth animation
    updateProgress: function (newValue, oldValue) {
        const progressMarker = document.querySelector('.progress-marker');
        if (!progressMarker) return;

        // Calculate new position
        const newPosition = this.calculatePosition(newValue);

        // Apply smooth transition
        progressMarker.style.transition = 'top 600ms cubic-bezier(0.25, 0.46, 0.45, 0.94)';
        progressMarker.style.top = `${newPosition}%`;

        // Update marker value with counting animation
        this.animateValueChange(progressMarker.querySelector('.marker-value'), oldValue, newValue);

        // Check for tier celebrations
        this.checkTierCelebrations(newValue, oldValue);
    },

    // Calculate position percentage for the ladder
    calculatePosition: function (value) {
        const tiers = document.querySelectorAll('.tier-row');
        if (tiers.length === 0) return 50;

        const maxTier = parseInt(tiers[0].querySelector('.tier-threshold').textContent);
        const minTier = 0;

        // Invert percentage (100% = top of ladder)
        const percentage = Math.max(0, Math.min(100, (value - minTier) / (maxTier - minTier) * 100));
        return 100 - percentage;
    },

    // Animate value changes with counting effect
    animateValueChange: function (element, fromValue, toValue) {
        if (!element) return;

        const duration = 800;
        const startTime = performance.now();

        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            // Easing function for smooth counting
            const easeOutCubic = 1 - Math.pow(1 - progress, 3);
            const currentValue = Math.floor(fromValue + (toValue - fromValue) * easeOutCubic);

            element.textContent = currentValue;

            if (progress < 1) {
                requestAnimationFrame(animate);
            } else {
                element.textContent = toValue;
            }
        };

        requestAnimationFrame(animate);
    },

    // Check for tier celebrations and trigger them
    checkTierCelebrations: function (newValue, oldValue) {
        const tiers = Array.from(document.querySelectorAll('.tier-row')).map(tier => {
            const threshold = parseInt(tier.querySelector('.tier-threshold').textContent);
            const label = tier.querySelector('.tier-label').textContent;
            return { threshold, label, element: tier };
        });

        // Find newly achieved tiers
        const newlyAchieved = tiers.filter(tier =>
            oldValue < tier.threshold && newValue >= tier.threshold
        );

        // Trigger celebrations for newly achieved tiers
        newlyAchieved.forEach(tier => {
            this.celebrateTierAchievement(tier);
        });
    },

    // Celebrate tier achievement
    celebrateTierAchievement: function (tier) {
        if (this.celebrationActive) return;
        this.celebrationActive = true;

        // Add celebration class to tier
        tier.element.classList.add('tier-celebration');

        // Create celebration particle effect
        this.createTierParticles(tier.element);

        // Show celebration message
        this.showCelebrationMessage(tier.label);

        // Trigger confetti
        this.triggerConfetti();

        // Reset celebration state after animation
        setTimeout(() => {
            tier.element.classList.remove('tier-celebration');
            this.celebrationActive = false;
        }, 2000);

        // Notify Blazor component
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('TriggerCelebration');
        }
    },

    // Create particle effect for tier achievement
    createTierParticles: function (tierElement) {
        const rect = tierElement.getBoundingClientRect();
        const particles = [];

        for (let i = 0; i < 15; i++) {
            const particle = document.createElement('div');
            particle.className = 'tier-particle';
            particle.style.cssText = `
                position: fixed;
                left: ${rect.left + rect.width / 2}px;
                top: ${rect.top + rect.height / 2}px;
                width: 8px;
                height: 8px;
                background: #ffd700;
                border-radius: 50%;
                pointer-events: none;
                z-index: 9999;
                transition: all 1s cubic-bezier(0.25, 0.46, 0.45, 0.94);
            `;

            document.body.appendChild(particle);

            // Animate particle
            setTimeout(() => {
                const angle = (Math.PI * 2 * i) / 15;
                const distance = 100 + Math.random() * 50;
                const x = Math.cos(angle) * distance;
                const y = Math.sin(angle) * distance;

                particle.style.transform = `translate(${x}px, ${y}px) scale(0)`;
                particle.style.opacity = '0';
            }, 50);

            setTimeout(() => {
                if (particle.parentNode) {
                    particle.parentNode.removeChild(particle);
                }
            }, 1500);
        }
    },

    // Show tier celebration message
    showCelebrationMessage: function (tierLabel) {
        const message = document.createElement('div');
        message.className = 'tier-celebration-message';
        message.textContent = `🎉 ${tierLabel} Tier Achieved!`;
        message.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) scale(0);
            background: #667eea;
            color: white;
            padding: 20px 30px;
            border-radius: 15px;
            font-size: 1.5rem;
            font-weight: bold;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
            z-index: 10000;
            pointer-events: none;
            transition: all 600ms cubic-bezier(0.68, -0.55, 0.265, 1.55);
        `;

        document.body.appendChild(message);

        // Animate in
        setTimeout(() => {
            message.style.transform = 'translate(-50%, -50%) scale(1)';
        }, 50);

        // Animate out
        setTimeout(() => {
            message.style.transform = 'translate(-50%, -50%) scale(0)';
            message.style.opacity = '0';
        }, 2000);

        // Remove from DOM
        setTimeout(() => {
            if (message.parentNode) {
                message.parentNode.removeChild(message);
            }
        }, 2600);
    },

    // Enhanced confetti for tier celebrations
    triggerConfetti: function () {
        const colors = ['#ff6b6b', '#4ecdc4', '#45b7d1', '#96ceb4', '#ffeaa7', '#dda0dd', '#98d8c8', '#ffd700'];
        const confettiCount = 80;

        for (let i = 0; i < confettiCount; i++) {
            const confetti = document.createElement('div');
            confetti.style.cssText = `
                position: fixed;
                left: ${Math.random() * 100}%;
                top: -10px;
                width: ${8 + Math.random() * 6}px;
                height: ${8 + Math.random() * 6}px;
                background: ${colors[Math.floor(Math.random() * colors.length)]};
                border-radius: ${Math.random() > 0.5 ? '50%' : '0'};
                z-index: 9999;
                pointer-events: none;
                transform: rotate(${Math.random() * 360}deg);
                transition: all ${2 + Math.random()}s cubic-bezier(0.25, 0.46, 0.45, 0.94);
            `;

            document.body.appendChild(confetti);

            // Animate confetti fall
            setTimeout(() => {
                confetti.style.transform = `translateY(${window.innerHeight + 50}px) rotate(${360 + Math.random() * 720}deg)`;
                confetti.style.opacity = '0';
            }, 100);

            // Clean up
            setTimeout(() => {
                if (confetti.parentNode) {
                    confetti.parentNode.removeChild(confetti);
                }
            }, 4000);
        }
    },

    // Setup physics-based progress marker movement
    setupProgressMarkerPhysics: function () {
        let lastPosition = 0;
        let velocity = 0;
        const friction = 0.85;
        const springStrength = 0.02;

        const animateMarker = () => {
            const marker = document.querySelector('.progress-marker');
            if (!marker) return;

            const targetTop = parseFloat(marker.style.top) || 50;
            const currentTop = lastPosition;

            // Spring physics
            const force = (targetTop - currentTop) * springStrength;
            velocity += force;
            velocity *= friction;

            lastPosition += velocity;

            // Apply position with sub-pixel precision
            if (Math.abs(velocity) > 0.01 || Math.abs(targetTop - currentTop) > 0.1) {
                marker.style.transform = `translateX(-50%) translateY(${velocity * 2}px)`;
                requestAnimationFrame(animateMarker);
            } else {
                marker.style.transform = 'translateX(-50%)';
            }
        };

        // Start physics animation
        requestAnimationFrame(animateMarker);
    },

    // Celebrate major achievements
    celebrate: function () {
        this.triggerConfetti();

        // Create celebratory wave animation
        const tiers = document.querySelectorAll('.tier-active');
        tiers.forEach((tier, index) => {
            setTimeout(() => {
                tier.style.transform = 'scale(1.05)';
                tier.style.boxShadow = '0 8px 25px rgba(34, 197, 94, 0.4)';

                setTimeout(() => {
                    tier.style.transform = '';
                    tier.style.boxShadow = '';
                }, 300);
            }, index * 100);
        });
    },

    // Handle resize for responsive updates
    handleResize: function () {
        // Recalculate positions on resize
        const marker = document.querySelector('.progress-marker');
        if (marker && this.dotNetRef) {
            // Notify Blazor component to recalculate
            this.dotNetRef.invokeMethodAsync('RecalculatePositions');
        }
    },

    // Cleanup when component is destroyed
    dispose: function () {
        this.dotNetRef = null;
        this.animationQueue = [];

        // Remove event listeners
        window.removeEventListener('resize', this.handleResize);

        // Clean up any remaining particles or animations
        document.querySelectorAll('.tier-particle, .tier-celebration-message').forEach(el => {
            if (el.parentNode) {
                el.parentNode.removeChild(el);
            }
        });
    }
};

// Setup global resize handler
window.addEventListener('resize', () => {
    if (window.progressLadder) {
        window.progressLadder.handleResize();
    }
});

// Add CSS for additional animations
const style = document.createElement('style');
style.textContent = `
    .tier-row:not(.revealed) {
        opacity: 0;
        transform: translateX(-20px);
    }

    .tier-row.revealed {
        opacity: 1;
        transform: translateX(0);
        transition: all 600ms cubic-bezier(0.25, 0.46, 0.45, 0.94);
    }

    .tier-celebration {
        animation: tier-celebration-pulse 2s ease-in-out;
    }

    @keyframes tier-celebration-pulse {
        0%, 100% { transform: scale(1); }
        25% { transform: scale(1.05); box-shadow: 0 8px 25px rgba(34, 197, 94, 0.6); }
        50% { transform: scale(1.02); }
        75% { transform: scale(1.05); box-shadow: 0 8px 25px rgba(34, 197, 94, 0.6); }
    }

    .progress-marker {
        will-change: transform;
    }

    @media (prefers-reduced-motion: reduce) {
        .tier-row.revealed,
        .tier-celebration,
        .progress-marker {
            transition: none;
            animation: none;
        }
    }
`;
document.head.appendChild(style);